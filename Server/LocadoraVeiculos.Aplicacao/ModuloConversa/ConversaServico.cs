using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloConversa;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCatalogo;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConversa;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloReserva;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConversa;

public record MensagemDto(
    Guid Id,
    Guid ConversaId,
    AutorMensagem Autor,
    string AutorNome,
    string Texto,
    DateTimeOffset EnviadaEmUtc,
    Guid? AutomovelId,
    string? DescricaoVeiculo
);

/// <summary>Uma linha da caixa de entrada: com quem é a conversa, a última mensagem e quantas faltam ler.</summary>
public record ConversaResumoDto(Guid Id, string Titulo, string UltimaMensagem, DateTimeOffset UltimaMensagemEmUtc, int NaoLidas);

public record ConversaDetalheDto(Guid Id, string Titulo, IReadOnlyList<MensagemDto> Mensagens);

/// <summary>Avisa as telas abertas (tempo real) de que chegou uma mensagem. A implementação fica na API (SignalR).</summary>
public interface IChatNotificador
{
    Task NovaMensagemAsync(Conversa conversa, MensagemDto mensagem, CancellationToken cancellationToken);
}

/// <summary>
/// Conversa entre clientes e locadoras. O cliente começa pelo carro (ou pelo pedido), a locadora responde pela caixa de entrada
/// (administrador e funcionários compartilham). Cada mensagem chega em tempo real e, se a outra pessoa ainda não leu nada,
/// também por e-mail (só no primeiro aviso, para não encher a caixa).
/// </summary>
public class ConversaServico(
    RepositorioConversaEmOrm repositorio,
    RepositorioReservaEmOrm repositorioReserva,
    RepositorioCatalogoEmOrm repositorioCatalogo,
    UserManager<Usuario> userManager,
    IEnviadorDeEmail enviadorDeEmail,
    IChatNotificador notificador,
    AppDbContext dbContext,
    IConfiguration configuracao,
    ILogger<ConversaServico> logger
)
{
    private const int MensagensPorTela = 200;

    // ---------------------------------------------------------------- cliente

    /// <summary>Começa (ou retoma) a conversa do cliente com a locadora do carro e envia a primeira mensagem.</summary>
    public async Task<Result<Guid>> IniciarComoClienteAsync(
        Guid usuarioId, Guid automovelId, string? texto, CancellationToken cancellationToken)
    {
        var validacao = ValidarTexto(texto);

        if (validacao.IsFailed)
            return Result.Fail(validacao.Errors);

        var carro = await repositorioReserva.SelecionarCarroAsync(automovelId);

        if (carro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(automovelId));

        Usuario? cliente = await userManager.FindByIdAsync(usuarioId.ToString());

        if (cliente is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não encontrado."));

        Conversa? conversa = await repositorio.SelecionarDoClientePorLocadoraAsync(usuarioId, carro.EmpresaId);

        if (conversa is null)
        {
            conversa = new Conversa
            {
                EmpresaId = carro.EmpresaId,
                UsuarioClienteId = usuarioId,
                ClienteNome = cliente.FullName,
                NomeLocadora = await repositorioCatalogo.SelecionarNomeDaLocadoraAsync(carro.EmpresaId)
            };

            await repositorio.CadastrarAsync(conversa);
        }

        await RegistrarAsync(
            conversa, AutorMensagem.Cliente, cliente.FullName, validacao.Value,
            automovelId, $"{carro.Marca} {carro.Modelo} ({carro.Ano})", cancellationToken);

        return Result.Ok(conversa.Id);
    }

    public async Task<Result<MensagemDto>> EnviarComoClienteAsync(
        Guid usuarioId, Guid conversaId, string? texto, Guid? automovelId, CancellationToken cancellationToken)
    {
        var validacao = ValidarTexto(texto);

        if (validacao.IsFailed)
            return Result.Fail(validacao.Errors);

        Conversa? conversa = await repositorio.SelecionarDoClientePorIdAsync(conversaId, usuarioId);

        if (conversa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(conversaId));

        // Falar de outro carro da mesma locadora: a mensagem leva a etiqueta do carro.
        string? descricao = null;

        if (automovelId is { } id)
        {
            var carro = await repositorioReserva.SelecionarCarroAsync(id);

            if (carro is not null && carro.EmpresaId == conversa.EmpresaId)
                descricao = $"{carro.Marca} {carro.Modelo} ({carro.Ano})";
            else
                automovelId = null;
        }

        Usuario? cliente = await userManager.FindByIdAsync(usuarioId.ToString());

        return Result.Ok(await RegistrarAsync(
            conversa, AutorMensagem.Cliente, cliente?.FullName ?? conversa.ClienteNome, validacao.Value,
            automovelId, descricao, cancellationToken));
    }

    public async Task<List<ConversaResumoDto>> ListarDoClienteAsync(Guid usuarioId) =>
        (await repositorio.ListarDoClienteAsync(usuarioId))
            .Select(c => new ConversaResumoDto(c.Id, c.NomeLocadora, c.UltimaMensagemResumo, c.UltimaMensagemEmUtc, c.NaoLidasCliente))
            .ToList();

    /// <summary>Abre a conversa: devolve as mensagens e marca como lidas as que a locadora mandou.</summary>
    public async Task<Result<ConversaDetalheDto>> AbrirComoClienteAsync(Guid usuarioId, Guid conversaId, CancellationToken cancellationToken)
    {
        Conversa? conversa = await repositorio.SelecionarDoClientePorIdAsync(conversaId, usuarioId);

        if (conversa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(conversaId));

        if (conversa.NaoLidasCliente > 0)
        {
            conversa.NaoLidasCliente = 0;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok(await DetalharAsync(conversa, conversa.NomeLocadora));
    }

    public Task<int> TotalNaoLidasDoClienteAsync(Guid usuarioId) => repositorio.TotalNaoLidasDoClienteAsync(usuarioId);

    // ---------------------------------------------------------------- locadora

    public async Task<Result<MensagemDto>> EnviarComoLocadoraAsync(
        Guid usuarioId, Guid conversaId, string? texto, CancellationToken cancellationToken)
    {
        var validacao = ValidarTexto(texto);

        if (validacao.IsFailed)
            return Result.Fail(validacao.Errors);

        Conversa? conversa = await repositorio.SelecionarDaLocadoraPorIdAsync(conversaId);

        if (conversa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(conversaId));

        Usuario? atendente = await userManager.FindByIdAsync(usuarioId.ToString());

        return Result.Ok(await RegistrarAsync(
            conversa, AutorMensagem.Locadora, atendente?.FullName ?? conversa.NomeLocadora, validacao.Value,
            null, null, cancellationToken));
    }

    public async Task<List<ConversaResumoDto>> ListarDaLocadoraAsync() =>
        (await repositorio.ListarDaLocadoraAsync())
            .Select(c => new ConversaResumoDto(c.Id, c.ClienteNome, c.UltimaMensagemResumo, c.UltimaMensagemEmUtc, c.NaoLidasLocadora))
            .ToList();

    public async Task<Result<ConversaDetalheDto>> AbrirComoLocadoraAsync(Guid conversaId, CancellationToken cancellationToken)
    {
        Conversa? conversa = await repositorio.SelecionarDaLocadoraPorIdAsync(conversaId);

        if (conversa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(conversaId));

        if (conversa.NaoLidasLocadora > 0)
        {
            conversa.NaoLidasLocadora = 0;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok(await DetalharAsync(conversa, conversa.ClienteNome));
    }

    public Task<int> TotalNaoLidasDaLocadoraAsync() => repositorio.TotalNaoLidasDaLocadoraAsync();

    // ---------------------------------------------------------------- comum

    private async Task<ConversaDetalheDto> DetalharAsync(Conversa conversa, string titulo)
    {
        var mensagens = await repositorio.UltimasMensagensAsync(conversa.Id, MensagensPorTela);

        return new ConversaDetalheDto(conversa.Id, titulo, mensagens.Select(Converter).ToList());
    }

    private static Result<string> ValidarTexto(string? texto)
    {
        string limpo = (texto ?? string.Empty).Trim();

        if (limpo.Length == 0)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("Escreva uma mensagem."));

        if (limpo.Length > Mensagem.TamanhoMaximo)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro($"A mensagem pode ter no máximo {Mensagem.TamanhoMaximo} caracteres."));

        return Result.Ok(limpo);
    }

    private static MensagemDto Converter(Mensagem m) =>
        new(m.Id, m.ConversaId, m.Autor, m.AutorNome, m.Texto, m.EnviadaEmUtc, m.AutomovelId, m.DescricaoVeiculo);

    private static string Resumo(string texto) =>
        texto.Length <= 120 ? texto : texto[..117] + "...";

    /// <summary>Grava a mensagem, atualiza a conversa, avisa as telas abertas e, no primeiro aviso, manda um e-mail.</summary>
    private async Task<MensagemDto> RegistrarAsync(
        Conversa conversa, AutorMensagem autor, string autorNome, string texto,
        Guid? automovelId, string? descricaoVeiculo, CancellationToken cancellationToken)
    {
        DateTimeOffset agora = DateTimeOffset.UtcNow;

        var mensagem = new Mensagem
        {
            ConversaId = conversa.Id,
            Autor = autor,
            AutorNome = autorNome,
            Texto = texto,
            EnviadaEmUtc = agora,
            AutomovelId = automovelId,
            DescricaoVeiculo = descricaoVeiculo
        };

        await repositorio.CadastrarAsync(mensagem);

        conversa.UltimaMensagemEmUtc = agora;
        conversa.UltimaMensagemResumo = Resumo(texto);

        bool primeiraNaoLida;

        if (autor == AutorMensagem.Cliente)
        {
            primeiraNaoLida = conversa.NaoLidasLocadora == 0;
            conversa.NaoLidasLocadora++;
        }
        else
        {
            primeiraNaoLida = conversa.NaoLidasCliente == 0;
            conversa.NaoLidasCliente++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        MensagemDto dto = Converter(mensagem);

        try
        {
            await notificador.NovaMensagemAsync(conversa, dto, cancellationToken);
        }
        catch (Exception ex)
        {
            // O tempo real é um extra: a mensagem já está gravada e aparece ao abrir a conversa.
            logger.LogError(ex, "Não foi possível avisar as telas abertas da conversa {Id}.", conversa.Id);
        }

        if (primeiraNaoLida)
            await AvisarPorEmailAsync(conversa, autor, autorNome, texto, cancellationToken);

        return dto;
    }

    private async Task AvisarPorEmailAsync(Conversa conversa, AutorMensagem autor, string autorNome, string texto, CancellationToken cancellationToken)
    {
        try
        {
            Guid destinatarioId = autor == AutorMensagem.Cliente ? conversa.EmpresaId : conversa.UsuarioClienteId;
            Usuario? destinatario = await userManager.FindByIdAsync(destinatarioId.ToString());

            if (destinatario?.Email is null || destinatario.ContaExcluidaEmUtc is not null)
                return;

            string de = autor == AutorMensagem.Cliente ? conversa.ClienteNome : conversa.NomeLocadora;
            string endereco = (configuracao["APP_URL"] ?? string.Empty).TrimEnd('/');
            string caminho = autor == AutorMensagem.Cliente ? "/conversas" : "/mensagens";
            string link = endereco.Length > 0 ? $"\n\nResponda em: {endereco}{caminho}" : "\n\nEntre no SerraDrive para responder.";

            await enviadorDeEmail.EnviarAsync(
                destinatario.Email,
                $"SerraDrive: nova mensagem de {de}",
                $"Olá, {destinatario.FullName}.\n\n{autorNome} escreveu:\n\"{Resumo(texto)}\"{link}\n\n" +
                "Você só recebe um aviso por conversa até abrir as mensagens.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Não foi possível avisar por e-mail sobre a mensagem da conversa {Id}.", conversa.Id);
        }
    }
}
