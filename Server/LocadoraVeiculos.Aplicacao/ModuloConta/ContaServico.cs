using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConta;

/// <summary>
/// Exclusão da própria conta (LGPD).
/// <list type="bullet">
/// <item>Cliente: imediata. A conta de login é anonimizada, os dados de "Meus dados" são apagados e o que ficou com as
/// locadoras (reservas e os cadastros criados a partir delas) perde os dados pessoais.</item>
/// <item>Locadora: agendada para <see cref="DiasDeCarencia"/> dias depois (dá para cancelar); depois, a empresa inteira some
/// e os dados pessoais de clientes, condutores e funcionários são anonimizados.</item>
/// </list>
/// A linha da conta de login nunca é apagada: todas as tabelas apontam para ela. Ela é anonimizada (e-mail liberado,
/// senha removida, bloqueada para sempre).
/// </summary>
public class ContaServico(
    AppDbContext dbContext,
    UserManager<Usuario> userManager,
    IEnviadorDeEmail enviadorDeEmail,
    ILogger<ContaServico> logger
)
{
    public const int DiasDeCarencia = 7;
    public const string PalavraDeConfirmacao = "EXCLUIR";
    public const string ContaExcluida = "Conta excluída";

    /// <summary>Texto único que ocupa o lugar de CPF/CNH apagados (os índices únicos não aceitam repetidos).</summary>
    private static string Marcador(Guid id) => "ANON-" + id.ToString("N")[..9];

    // ---------------------------------------------------------------- cliente

    public async Task<Result> ExcluirContaDeClienteAsync(
        Guid usuarioId, string? senha, string? confirmacao, CancellationToken cancellationToken)
    {
        if (!string.Equals(confirmacao?.Trim(), PalavraDeConfirmacao, StringComparison.OrdinalIgnoreCase))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro($"Digite {PalavraDeConfirmacao} para confirmar a exclusão."));

        var verificacao = await ObterEConferirSenhaAsync(usuarioId, senha);

        if (verificacao.IsFailed)
            return Result.Fail(verificacao.Errors);

        Usuario usuario = verificacao.Value;

        if (!await userManager.IsInRoleAsync(usuario, CargoUsuario.Cliente.ToString()))
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Esta exclusão é só para contas de cliente."));

        string email = usuario.Email!;
        string nome = usuario.FullName;

        List<Reserva> reservas = await dbContext.Reservas.IgnoreQueryFilters()
            .Where(r => r.UsuarioClienteId == usuario.Id)
            .ToListAsync(cancellationToken);

        List<Guid> aluguelIds = reservas.Where(r => r.AluguelId != null).Select(r => r.AluguelId!.Value).ToList();

        List<Aluguel> alugueis = await dbContext.Alugueis.IgnoreQueryFilters()
            .Where(a => aluguelIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        // Quem ainda está com um carro (ou tem um aluguel marcado) não exclui a conta: a locadora precisa do contato.
        if (alugueis.Any(a => !a.Excluido && a.Status == StatusAluguel.EmAberto))
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(
                "Você tem um aluguel em aberto. Conclua-o (ou fale com a locadora) antes de excluir a conta."));

        // Pedidos que ainda esperavam resposta deixam de existir.
        foreach (Reserva pendente in reservas.Where(r => r.EstaPendente))
            pendente.Cancelar();

        // O que as locadoras guardaram a partir dos pedidos deste cliente perde os dados pessoais.
        List<Guid> clienteIds = alugueis.Select(a => a.ClienteId).Distinct().ToList();
        List<Guid> condutorIds = alugueis.Select(a => a.CondutorId).Distinct().ToList();

        var clientes = await dbContext.Clientes.IgnoreQueryFilters().Where(c => clienteIds.Contains(c.Id)).ToListAsync(cancellationToken);
        var condutores = await dbContext.Condutores.IgnoreQueryFilters().Where(c => condutorIds.Contains(c.Id)).ToListAsync(cancellationToken);

        clientes.ForEach(AnonimizarCliente);
        condutores.ForEach(AnonimizarCondutor);
        alugueis.ForEach(AnonimizarAluguel);
        reservas.ForEach(AnonimizarReserva);

        PerfilClienteRemover(usuario.Id);

        await AnonimizarContaAsync(usuario, DateTime.UtcNow, cancellationToken);

        await AvisarAsync(
            email,
            "SerraDrive: sua conta foi excluída",
            $"Olá, {nome}.\n\nSua conta no SerraDrive foi excluída e os seus dados pessoais foram removidos.\n" +
            "O histórico que as locadoras mantêm das suas reservas ficou sem os seus dados pessoais.\n\n" +
            "Se não foi você que pediu, responda a este e-mail ou fale com a gente pelos contatos do site.",
            cancellationToken);

        return Result.Ok();
    }

    private void PerfilClienteRemover(Guid usuarioId)
    {
        var perfil = dbContext.PerfisClientes.FirstOrDefault(p => p.UsuarioId == usuarioId);

        if (perfil is not null)
            dbContext.PerfisClientes.Remove(perfil);
    }

    // ---------------------------------------------------------------- locadora

    public async Task<Result<DateTime>> AgendarExclusaoDaLocadoraAsync(
        Guid usuarioId, string? senha, string? nomeDaEmpresa, CancellationToken cancellationToken)
    {
        var verificacao = await ObterEConferirSenhaAsync(usuarioId, senha);

        if (verificacao.IsFailed)
            return Result.Fail(verificacao.Errors);

        Usuario usuario = verificacao.Value;

        if (!await userManager.IsInRoleAsync(usuario, CargoUsuario.Empresa.ToString()))
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Só o administrador da locadora pode pedir a exclusão."));

        if (!string.Equals(nomeDaEmpresa?.Trim(), usuario.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O nome digitado não confere com o nome da conta."));

        if (usuario.ExclusaoAgendadaParaUtc is { } jaAgendada)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro($"A exclusão já está agendada para {jaAgendada:dd/MM/yyyy}."));

        if (await TemPendenciasAsync(usuario.Id, cancellationToken) is { } pendencia)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(pendencia));

        DateTime data = DateTime.UtcNow.AddDays(DiasDeCarencia);

        usuario.ExclusaoAgendadaParaUtc = data;
        await dbContext.SaveChangesAsync(cancellationToken);

        await AvisarAsync(
            usuario.Email!,
            "SerraDrive: a exclusão da sua locadora foi agendada",
            $"Olá, {usuario.FullName}.\n\nRecebemos o pedido para excluir a sua locadora. A exclusão acontecerá em {data:dd/MM/yyyy}.\n" +
            "Nesse dia serão removidos os dados da empresa e as contas dos funcionários, e os dados pessoais de clientes, " +
            "condutores e funcionários serão anonimizados.\n\n" +
            "Mudou de ideia? Entre no sistema e clique em \"Cancelar exclusão\" antes dessa data.\n" +
            "Se não foi você que pediu, entre agora, cancele a exclusão e troque a sua senha.",
            cancellationToken);

        return Result.Ok(data);
    }

    public async Task<Result> CancelarExclusaoDaLocadoraAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        Usuario? usuario = await userManager.FindByIdAsync(usuarioId.ToString());

        if (usuario is null || usuario.ContaExcluidaEmUtc is not null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Conta não encontrada."));

        if (usuario.ExclusaoAgendadaParaUtc is null)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Não há exclusão agendada."));

        usuario.ExclusaoAgendadaParaUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await AvisarAsync(
            usuario.Email!,
            "SerraDrive: a exclusão da sua locadora foi cancelada",
            $"Olá, {usuario.FullName}.\n\nA exclusão da sua locadora foi cancelada. Nada será removido.",
            cancellationToken);

        return Result.Ok();
    }

    public async Task<DateTime?> ObterExclusaoAgendadaAsync(Guid usuarioId)
    {
        Usuario? usuario = await userManager.FindByIdAsync(usuarioId.ToString());

        return usuario?.ExclusaoAgendadaParaUtc;
    }

    /// <summary>Executa as exclusões de locadora cujo prazo já passou. Devolve quantas foram concluídas.</summary>
    public async Task<int> ExecutarAgendadasAsync(DateTime agora, CancellationToken cancellationToken)
    {
        List<Guid> ids = await dbContext.Users
            .Where(u => u.ExclusaoAgendadaParaUtc != null && u.ExclusaoAgendadaParaUtc <= agora && u.ContaExcluidaEmUtc == null)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        int concluidas = 0;

        foreach (Guid id in ids)
        {
            try
            {
                if (await ExecutarExclusaoDaLocadoraAsync(id, agora, cancellationToken))
                    concluidas++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Não foi possível executar a exclusão agendada da locadora {Id}.", id);

                dbContext.ChangeTracker.Clear();
            }
        }

        return concluidas;
    }

    private async Task<bool> ExecutarExclusaoDaLocadoraAsync(Guid empresaId, DateTime agora, CancellationToken cancellationToken)
    {
        Usuario empresa = (await userManager.FindByIdAsync(empresaId.ToString()))!;
        string email = empresa.Email!;
        string nome = empresa.FullName;

        // Chegou alguma pendência depois do pedido: adia um dia em vez de apagar com aluguel em andamento.
        if (await TemPendenciasAsync(empresaId, cancellationToken) is { } pendencia)
        {
            empresa.ExclusaoAgendadaParaUtc = agora.AddDays(1);
            await dbContext.SaveChangesAsync(cancellationToken);

            await AvisarAsync(
                email,
                "SerraDrive: a exclusão da sua locadora foi adiada",
                $"Olá, {nome}.\n\nA exclusão da sua locadora estava marcada para hoje, mas foi adiada em um dia: {pendencia}\n" +
                "Resolva a pendência (ou cancele a exclusão no sistema).",
                cancellationToken);

            return false;
        }

        // Tudo da empresa, inclusive o que já estava na lixeira lógica.
        var clientes = await dbContext.Clientes.IgnoreQueryFilters().Where(c => c.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var condutores = await dbContext.Condutores.IgnoreQueryFilters().Where(c => c.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var alugueis = await dbContext.Alugueis.IgnoreQueryFilters().Where(a => a.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var reservas = await dbContext.Reservas.IgnoreQueryFilters().Where(r => r.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var funcionarios = await dbContext.Funcionarios.IgnoreQueryFilters().Where(f => f.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var automoveis = await dbContext.Automoveis.IgnoreQueryFilters().Where(a => a.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var grupos = await dbContext.GruposVeiculos.IgnoreQueryFilters().Where(g => g.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var planos = await dbContext.PlanosCobranca.IgnoreQueryFilters().Where(p => p.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var taxas = await dbContext.TaxasServicos.IgnoreQueryFilters().Where(t => t.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var parceiros = await dbContext.Parceiros.IgnoreQueryFilters().Where(p => p.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var cupons = await dbContext.Cupons.IgnoreQueryFilters().Where(c => c.EmpresaId == empresaId).ToListAsync(cancellationToken);
        var configuracoes = await dbContext.Configuracoes.IgnoreQueryFilters().Where(c => c.EmpresaId == empresaId).ToListAsync(cancellationToken);

        clientes.ForEach(c => { AnonimizarCliente(c); c.Excluir(); });
        condutores.ForEach(c => { AnonimizarCondutor(c); c.Excluir(); });
        alugueis.ForEach(a => { AnonimizarAluguel(a); a.Excluir(); });
        reservas.ForEach(r => { AnonimizarReserva(r); r.Excluir(); });
        automoveis.ForEach(a => { a.Foto = null; a.FotoNomeArquivo = null; a.FotoTipoConteudo = null; a.Excluir(); });
        grupos.ForEach(g => g.Excluir());
        planos.ForEach(p => p.Excluir());
        taxas.ForEach(t => t.Excluir());
        parceiros.ForEach(p => p.Excluir());
        cupons.ForEach(c => c.Excluir());
        configuracoes.ForEach(c => { c.NomeFantasia = null; c.Telefone = null; c.Excluir(); });

        // Funcionários: dados pessoais apagados e a conta de login anonimizada (perdem o acesso na hora).
        foreach (var funcionario in funcionarios)
        {
            funcionario.NomeCompleto = ContaExcluida;
            funcionario.Cpf = Marcador(funcionario.Id);
            funcionario.Email = $"excluido-{funcionario.Id:N}@excluido.invalid";
            funcionario.Salario = 0;
            funcionario.Ativo = false;
            funcionario.Excluir();

            Usuario? contaDoFuncionario = await userManager.FindByIdAsync(funcionario.UsuarioId.ToString());

            if (contaDoFuncionario is not null && contaDoFuncionario.ContaExcluidaEmUtc is null)
                MarcarContaComoExcluida(contaDoFuncionario, agora);
        }

        MarcarContaComoExcluida(empresa, agora);

        await dbContext.SaveChangesAsync(cancellationToken);

        await AvisarAsync(
            email,
            "SerraDrive: a sua locadora foi excluída",
            $"Olá, {nome}.\n\nComo você pediu, a sua locadora foi excluída: os dados da empresa foram removidos, as contas de acesso " +
            "foram encerradas e os dados pessoais de clientes, condutores e funcionários foram anonimizados.",
            cancellationToken);

        return true;
    }

    /// <summary>Texto da pendência que impede a exclusão (aluguel em aberto ou reserva esperando resposta), ou nulo.</summary>
    private async Task<string?> TemPendenciasAsync(Guid empresaId, CancellationToken cancellationToken)
    {
        if (await dbContext.Alugueis.IgnoreQueryFilters()
                .AnyAsync(a => a.EmpresaId == empresaId && !a.Excluido && a.Status == StatusAluguel.EmAberto, cancellationToken))
            return "Há aluguéis em aberto. Conclua as devoluções antes de excluir a locadora.";

        if (await dbContext.Reservas.IgnoreQueryFilters()
                .AnyAsync(r => r.EmpresaId == empresaId && !r.Excluido && r.Status == StatusReserva.Pendente, cancellationToken))
            return "Há reservas esperando resposta. Aprove ou recuse cada uma antes de excluir a locadora.";

        return null;
    }

    // ---------------------------------------------------------------- comum

    private async Task<Result<Usuario>> ObterEConferirSenhaAsync(Guid usuarioId, string? senha)
    {
        Usuario? usuario = await userManager.FindByIdAsync(usuarioId.ToString());

        if (usuario is null || usuario.ContaExcluidaEmUtc is not null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Conta não encontrada."));

        if (string.IsNullOrEmpty(senha) || !await userManager.CheckPasswordAsync(usuario, senha))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A senha informada está incorreta."));

        return Result.Ok(usuario);
    }

    private async Task AnonimizarContaAsync(Usuario usuario, DateTime agora, CancellationToken cancellationToken)
    {
        MarcarContaComoExcluida(usuario, agora);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Troca os dados da conta de login por valores sem significado, tira a senha, bloqueia para sempre, derruba as
    /// sessões abertas e libera o e-mail para um novo cadastro.
    /// </summary>
    private void MarcarContaComoExcluida(Usuario usuario, DateTime agora)
    {
        string anonimo = $"excluido-{usuario.Id:N}";

        usuario.UserName = anonimo;
        usuario.NormalizedUserName = anonimo.ToUpperInvariant();
        usuario.Email = $"{anonimo}@excluido.invalid";
        usuario.NormalizedEmail = usuario.Email.ToUpperInvariant();
        usuario.EmailConfirmed = false;
        usuario.PhoneNumber = null;
        usuario.FullName = ContaExcluida;
        usuario.PasswordHash = null;
        usuario.SecurityStamp = Guid.NewGuid().ToString();
        usuario.AccessTokenVersionId = Guid.NewGuid();
        usuario.LockoutEnabled = true;
        usuario.LockoutEnd = DateTimeOffset.MaxValue;
        usuario.ExclusaoAgendadaParaUtc = null;
        usuario.ContaExcluidaEmUtc = agora;

        dbContext.RefreshTokens.RemoveRange(dbContext.RefreshTokens.Where(t => t.UsuarioId == usuario.Id));
    }

    private static void AnonimizarCliente(Dominio.ModuloCliente.Cliente c)
    {
        c.Nome = ContaExcluida;
        c.Email = $"excluido-{c.Id:N}@excluido.invalid";
        c.Telefone = string.Empty;
        c.Cpf = c.Cpf is null ? null : Marcador(c.Id);
        c.Rg = null;
        c.Cnh = null;
        c.Estado = string.Empty;
        c.Cidade = string.Empty;
        c.Bairro = string.Empty;
        c.Rua = string.Empty;
        c.Numero = string.Empty;
    }

    private static void AnonimizarCondutor(Dominio.ModuloCondutor.Condutor c)
    {
        c.Nome = ContaExcluida;
        c.Email = $"excluido-{c.Id:N}@excluido.invalid";
        c.Telefone = string.Empty;
        c.Cpf = Marcador(c.Id);
        c.Cnh = string.Empty;
        c.ValidadeCnh = default;
    }

    private static void AnonimizarAluguel(Aluguel a)
    {
        a.NomeCliente = ContaExcluida;
        a.NomeCondutor = ContaExcluida;
    }

    private static void AnonimizarReserva(Reserva r)
    {
        r.ClienteNome = ContaExcluida;
        r.ClienteEmail = string.Empty;
        r.ClienteTelefone = string.Empty;
        r.ClienteCpf = string.Empty;
        r.ClienteRg = string.Empty;
        r.ClienteCnh = string.Empty;
        r.ClienteValidadeCnh = default;
        r.ClienteEstado = string.Empty;
        r.ClienteCidade = string.Empty;
        r.ClienteBairro = string.Empty;
        r.ClienteRua = string.Empty;
        r.ClienteNumero = string.Empty;
    }

    private async Task AvisarAsync(string para, string assunto, string corpo, CancellationToken cancellationToken)
    {
        try
        {
            await enviadorDeEmail.EnviarAsync(para, assunto, corpo, cancellationToken);
        }
        catch (Exception ex)
        {
            // O aviso é um extra: se falhar, a exclusão já aconteceu.
            logger.LogError(ex, "Não foi possível enviar o e-mail sobre a exclusão da conta.");
        }
    }
}
