using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Handlers;
using LocadoraDeVeiculos.Aplicacao.ModuloReserva.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Dominio.ModuloFuncionario;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConfiguracao;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloFuncionario;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPerfilCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloReserva;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DominioPerfil = LocadoraDeVeiculos.Dominio.ModuloPerfilCliente.PerfilCliente;

namespace LocadoraDeVeiculos.Aplicacao.ModuloReserva.Handlers;

internal static class ReservaMapeamento
{
    public static ReservaDto ParaDto(this Reserva r, string? telefoneDaLocadora = null) => new(
        r.Id,
        r.Status,
        r.NomeLocadora,
        r.Status == StatusReserva.Aprovada ? telefoneDaLocadora : null,
        r.AutomovelId,
        r.DescricaoVeiculo,
        r.NomeGrupo,
        r.Retirada,
        r.Devolucao,
        r.Dias,
        r.TipoPlano,
        r.TipoSeguro,
        r.ValorDiarias,
        r.ValorSeguro,
        r.ValorCaucao,
        r.ValorTotalPrevisto,
        r.MotivoRecusa,
        r.AluguelId,
        r.CriadoEmUtc,
        r.RespondidaEmUtc,
        r.ClienteNome,
        r.ClienteEmail,
        r.ClienteTelefone,
        r.ClienteCpf,
        r.ClienteCnh,
        r.ClienteValidadeCnh,
        r.ClienteCidade,
        r.ClienteEstado,
        r.ValorTaxas,
        r.ValorDesconto,
        r.Taxas
            .Select(t => new TaxaDaReservaDto(t.TaxaServicoId, t.Nome, t.PrecoUnitario, t.TipoCobranca,
                t.TipoCobranca == TipoCobrancaTaxa.Fixo ? t.PrecoUnitario : t.PrecoUnitario * r.Dias))
            .ToList(),
        r.CupomNome,
        r.CupomId is null ? null : r.CupomTipo,
        r.CupomValor
    );
}

// =====================================================================================
// Lado do cliente
// =====================================================================================

public class CalcularReservaQueryHandler(
    ReservaServico servico,
    RepositorioPerfilClienteEmOrm repositorioPerfil,
    ITenantProvider tenantProvider
) : IRequestHandler<CalcularReservaQuery, Result<EstimativaReservaDto>>
{
    public async Task<Result<EstimativaReservaDto>> Handle(CalcularReservaQuery query, CancellationToken cancellationToken)
    {
        // O CPF e o usuário servem para saber se o cliente já usou o cupom nesta locadora.
        Guid? usuarioId = tenantProvider.UsuarioId;
        var perfil = usuarioId is { } id ? await repositorioPerfil.SelecionarAsync(id) : null;

        Result<PreparoReserva> preparo = await servico.PrepararAsync(query.Dados, usuarioId, perfil?.Cpf);

        return preparo.IsFailed ? Result.Fail(preparo.Errors) : Result.Ok(preparo.Value.Estimativa);
    }
}

public class CriarReservaCommandHandler(
    AppDbContext appDbContext,
    RepositorioReservaEmOrm repositorioReserva,
    RepositorioPerfilClienteEmOrm repositorioPerfil,
    ReservaServico servico,
    UserManager<Usuario> userManager,
    ITenantProvider tenantProvider,
    ILogger<CriarReservaCommandHandler> logger
) : IRequestHandler<CriarReservaCommand, Result<CriarReservaResult>>
{
    private const int MaximoDePendentes = 5;

    public async Task<Result<CriarReservaResult>> Handle(CriarReservaCommand command, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não identificado."));

        DadosReservaDto d = command.Dados;

        DominioPerfil? perfil = await repositorioPerfil.SelecionarAsync(usuarioId);

        Result<PreparoReserva> preparo = await servico.PrepararAsync(d, usuarioId, perfil?.Cpf);

        if (preparo.IsFailed)
            return Result.Fail(preparo.Errors);

        if (perfil is null)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                "Preencha seus dados (CPF, CNH, contato e endereço) em Meus dados antes de reservar."));

        // A CNH precisa valer até a devolução (mesma regra do aluguel).
        if (perfil.ValidadeCnh < d.Devolucao)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                $"Sua CNH vence em {perfil.ValidadeCnh:dd/MM/yyyy}, antes da devolução. Atualize seus dados ou escolha outras datas."));

        if (await repositorioReserva.ContarPendentesDoClienteAsync(usuarioId) >= MaximoDePendentes)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(
                $"Você já tem {MaximoDePendentes} pedidos aguardando resposta. Espere as locadoras responderem ou cancele algum."));

        if (await repositorioReserva.ExistePendenteSobrepostaAsync(usuarioId, d.AutomovelId, d.Retirada, d.Devolucao))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                "Você já tem um pedido aguardando resposta para este carro neste período."));

        Usuario? usuario = await userManager.FindByIdAsync(usuarioId.ToString());

        if (usuario is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não encontrado."));

        try
        {
            PreparoReserva p = preparo.Value;

            Reserva reserva = new()
            {
                EmpresaId = p.Carro.EmpresaId,
                UsuarioClienteId = usuarioId,
                AutomovelId = p.Carro.AutomovelId,
                NomeLocadora = p.NomeLocadora,
                DescricaoVeiculo = p.DescricaoVeiculo,
                NomeGrupo = p.Carro.NomeGrupo,
                Retirada = d.Retirada,
                Devolucao = d.Devolucao,
                TipoPlano = d.TipoPlano,
                TipoSeguro = d.TipoSeguro,
                Dias = p.Estimativa.Dias,
                ValorDiarias = p.Estimativa.ValorDiarias,
                ValorSeguro = p.Estimativa.ValorSeguro,
                ValorCaucao = p.Estimativa.ValorCaucao,
                ValorTotalPrevisto = p.Estimativa.ValorTotalPrevisto,
                ValorTaxas = p.Estimativa.ValorTaxas,
                ValorDesconto = p.Estimativa.ValorDesconto,
                Taxas = p.Taxas.ToList(),
                CupomId = p.Cupom?.Id,
                CupomNome = p.Cupom?.Nome,
                CupomTipo = p.Cupom?.Tipo ?? TipoDesconto.Valor,
                CupomValor = p.Cupom?.Valor ?? 0m,
                ClienteNome = usuario.FullName,
                ClienteEmail = usuario.Email ?? string.Empty,
                ClienteTelefone = perfil.Telefone,
                ClienteCpf = perfil.Cpf,
                ClienteRg = perfil.Rg,
                ClienteCnh = perfil.Cnh,
                ClienteValidadeCnh = perfil.ValidadeCnh,
                ClienteEstado = perfil.Estado,
                ClienteCidade = perfil.Cidade,
                ClienteBairro = perfil.Bairro,
                ClienteRua = perfil.Rua,
                ClienteNumero = perfil.Numero
            };

            await repositorioReserva.CadastrarAsync(reserva);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CriarReservaResult(reserva.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o pedido de reserva {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarMinhasReservasQueryHandler(
    RepositorioReservaEmOrm repositorioReserva,
    ITenantProvider tenantProvider
) : IRequestHandler<SelecionarMinhasReservasQuery, Result<SelecionarReservasResult>>
{
    public async Task<Result<SelecionarReservasResult>> Handle(SelecionarMinhasReservasQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não identificado."));

        List<Reserva> reservas = await repositorioReserva.SelecionarDoClienteAsync(usuarioId);

        // O telefone da locadora só é mostrado quando a reserva foi aprovada.
        var telefones = await repositorioReserva.TelefonesDasLocadorasAsync(
            reservas.Where(r => r.Status == StatusReserva.Aprovada).Select(r => r.EmpresaId).Distinct().ToList());

        return Result.Ok(new SelecionarReservasResult(
            reservas.Select(r => r.ParaDto(telefones.GetValueOrDefault(r.EmpresaId))).ToList()));
    }
}

public class CancelarMinhaReservaCommandHandler(
    AppDbContext appDbContext,
    RepositorioReservaEmOrm repositorioReserva,
    ITenantProvider tenantProvider
) : IRequestHandler<CancelarMinhaReservaCommand, Result<ReservaDto>>
{
    public async Task<Result<ReservaDto>> Handle(CancelarMinhaReservaCommand command, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não identificado."));

        // Só acha pedidos do próprio cliente: o de outra pessoa nem aparece (404).
        Reserva? reserva = await repositorioReserva.SelecionarDoClientePorIdAsync(command.Id, usuarioId);

        if (reserva is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (!reserva.EstaPendente)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(
                reserva.Status == StatusReserva.Aprovada
                    ? "Este pedido já foi aprovado. Para cancelar, fale com a locadora."
                    : "Este pedido já foi respondido ou cancelado."));

        reserva.Cancelar();

        await appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(reserva.ParaDto());
    }
}

// =====================================================================================
// Lado da locadora
// =====================================================================================

public class SelecionarReservasRecebidasQueryHandler(
    RepositorioReservaEmOrm repositorioReserva
) : IRequestHandler<SelecionarReservasRecebidasQuery, Result<SelecionarReservasResult>>
{
    public async Task<Result<SelecionarReservasResult>> Handle(SelecionarReservasRecebidasQuery query, CancellationToken cancellationToken)
    {
        List<Reserva> reservas = await repositorioReserva.SelecionarDaLocadoraAsync(query.Status);

        return Result.Ok(new SelecionarReservasResult(reservas.Select(r => r.ParaDto()).ToList()));
    }
}

public class ContarReservasPendentesQueryHandler(
    RepositorioReservaEmOrm repositorioReserva
) : IRequestHandler<ContarReservasPendentesQuery, Result<int>>
{
    public async Task<Result<int>> Handle(ContarReservasPendentesQuery query, CancellationToken cancellationToken) =>
        Result.Ok(await repositorioReserva.ContarPendentesDaLocadoraAsync());
}

public class AprovarReservaCommandHandler(
    AppDbContext appDbContext,
    RepositorioReservaEmOrm repositorioReserva,
    RepositorioClienteEmOrm repositorioCliente,
    RepositorioCondutorEmOrm repositorioCondutor,
    RepositorioAluguelEmOrm repositorioAluguel,
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    RepositorioConfiguracaoEmOrm repositorioConfiguracao,
    AluguelServico aluguelServico,
    ClienteDoAluguelServico clienteDoAluguel,
    IEnviadorDeEmail enviadorDeEmail,
    ITenantProvider tenantProvider,
    ILogger<AprovarReservaCommandHandler> logger
) : IRequestHandler<AprovarReservaCommand, Result<ReservaDto>>
{
    public async Task<Result<ReservaDto>> Handle(AprovarReservaCommand command, CancellationToken cancellationToken)
    {
        Reserva? reserva = await repositorioReserva.SelecionarPorIdAsync(command.Id);

        if (reserva is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (!reserva.EstaPendente)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Este pedido já foi respondido ou cancelado."));

        if (command.KmInicial is < 0 or > 2_000_000)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A quilometragem inicial informada é inválida."));

        Cliente? clienteCriado = null;
        Condutor? condutorCriado = null;

        try
        {
            Guid empresaId = tenantProvider.EmpresaId.GetValueOrDefault();

            // 1) O cliente da locadora: reaproveita quem já tem o mesmo CPF; senão cadastra com os dados do pedido.
            Cliente? cliente = await repositorioCliente.SelecionarPorCpfAsync(reserva.ClienteCpf);

            if (cliente is null)
            {
                cliente = ClienteRegras.Montar(
                    TipoCliente.PessoaFisica,
                    empresaId,
                    new DadosClienteDto(
                        reserva.ClienteNome, reserva.ClienteEmail, reserva.ClienteTelefone,
                        reserva.ClienteCpf, reserva.ClienteRg, reserva.ClienteCnh, null,
                        reserva.ClienteEstado, reserva.ClienteCidade, reserva.ClienteBairro,
                        reserva.ClienteRua, reserva.ClienteNumero, null),
                    null);

                await repositorioCliente.CadastrarAsync(cliente);
                clienteCriado = cliente;
            }

            // 2) O condutor: a própria pessoa, com a CNH do pedido.
            Condutor? condutor = await repositorioCondutor.SelecionarPorCpfNoClienteAsync(cliente.Id, reserva.ClienteCpf);

            if (condutor is null)
            {
                condutor = new Condutor(
                    empresaId, cliente.Id, reserva.ClienteNome, reserva.ClienteEmail, reserva.ClienteTelefone,
                    reserva.ClienteCpf, reserva.ClienteCnh, reserva.ClienteValidadeCnh);

                await repositorioCondutor.CadastrarAsync(condutor);
                condutorCriado = condutor;
            }
            else
            {
                condutor.Cnh = reserva.ClienteCnh;
                condutor.ValidadeCnh = reserva.ClienteValidadeCnh;
            }

            // Salva antes para o aluguel enxergar o cliente e o condutor (se o aluguel falhar, quem foi criado aqui é desfeito:
            // o cliente só passa a existir na locadora quando faz um aluguel).
            await appDbContext.SaveChangesAsync(cancellationToken);

            // 3) O aluguel, com as mesmas regras de qualquer aluguel (disponibilidade, CNH, plano, seguro).
            Result<Aluguel> montagem = await aluguelServico.MontarAsync(
                new DadosAluguelDto(
                    condutor.Id, reserva.AutomovelId, reserva.TipoPlano, reserva.Retirada, reserva.Devolucao,
                    command.KmInicial, reserva.TipoSeguro,
                    reserva.Taxas.Select(t => t.TaxaServicoId).ToList(), reserva.CupomId),
                apenasPreco: false);

            if (montagem.IsFailed)
            {
                await DesfazerNovosAsync(clienteCriado, condutorCriado, cancellationToken);

                return Result.Fail(montagem.Errors);
            }

            Aluguel aluguel = montagem.Value;

            if (tenantProvider.EstaNoCargo(CargoUsuario.Funcionario.ToString()) && tenantProvider.UsuarioId is { } usuarioId)
            {
                Funcionario? funcionario = await repositorioFuncionario.SelecionarPorUsuarioIdAsync(usuarioId);

                aluguel.FuncionarioId = funcionario?.Id;
            }

            await repositorioAluguel.CadastrarAsync(aluguel);

            reserva.Aprovar(aluguel.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            Configuracao? configuracao = await repositorioConfiguracao.SelecionarDaEmpresaAsync();

            await AvisarClienteAsync(reserva, configuracao?.Telefone, cancellationToken);

            return Result.Ok(reserva.ParaDto(configuracao?.Telefone));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro ao aprovar a reserva {@Command}.", command);

            await DesfazerNovosAsync(clienteCriado, condutorCriado, cancellationToken);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }

    /// <summary>Quando o aluguel não sai, o cliente criado só para ele não fica na locadora.</summary>
    private async Task DesfazerNovosAsync(Cliente? cliente, Condutor? condutor, CancellationToken cancellationToken)
    {
        if (cliente is null || condutor is null) return;

        await clienteDoAluguel.DesfazerAsync(new ClienteECondutorNovos(cliente, condutor), cancellationToken);
    }

    private async Task AvisarClienteAsync(Reserva reserva, string? telefone, CancellationToken cancellationToken)
    {
        try
        {
            string contato = string.IsNullOrWhiteSpace(telefone) ? "" : $"\nFale com a locadora: {telefone}";

            await enviadorDeEmail.EnviarAsync(
                reserva.ClienteEmail,
                $"SerraDrive: sua reserva foi aprovada ({reserva.NomeLocadora})",
                $"Olá, {reserva.ClienteNome}.\n\n" +
                $"A locadora {reserva.NomeLocadora} aprovou a sua reserva do {reserva.DescricaoVeiculo}, " +
                $"de {reserva.Retirada:dd/MM/yyyy} a {reserva.Devolucao:dd/MM/yyyy}.\n" +
                $"Valor previsto: R$ {reserva.ValorTotalPrevisto:N2} (já com a caução de R$ {reserva.ValorCaucao:N2}).{contato}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            // O aviso por e-mail é um extra: se falhar, a reserva continua aprovada.
            logger.LogError(ex, "Não foi possível avisar o cliente por e-mail.");
        }
    }
}

public class RecusarReservaCommandHandler(
    AppDbContext appDbContext,
    RepositorioReservaEmOrm repositorioReserva,
    IEnviadorDeEmail enviadorDeEmail,
    ILogger<RecusarReservaCommandHandler> logger
) : IRequestHandler<RecusarReservaCommand, Result<ReservaDto>>
{
    public async Task<Result<ReservaDto>> Handle(RecusarReservaCommand command, CancellationToken cancellationToken)
    {
        Reserva? reserva = await repositorioReserva.SelecionarPorIdAsync(command.Id);

        if (reserva is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (!reserva.EstaPendente)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Este pedido já foi respondido ou cancelado."));

        if (command.Motivo is { Length: > 300 })
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O motivo deve conter no máximo 300 caracteres."));

        reserva.Recusar(command.Motivo);

        await appDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            string motivo = reserva.MotivoRecusa is null ? "" : $"\nMotivo: {reserva.MotivoRecusa}";

            await enviadorDeEmail.EnviarAsync(
                reserva.ClienteEmail,
                $"SerraDrive: sua reserva não foi aprovada ({reserva.NomeLocadora})",
                $"Olá, {reserva.ClienteNome}.\n\n" +
                $"A locadora {reserva.NomeLocadora} não pôde atender o seu pedido do {reserva.DescricaoVeiculo}, " +
                $"de {reserva.Retirada:dd/MM/yyyy} a {reserva.Devolucao:dd/MM/yyyy}.{motivo}\n\n" +
                "Procure outro carro no catálogo.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Não foi possível avisar o cliente por e-mail.");
        }

        return Result.Ok(reserva.ParaDto());
    }
}
