using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloGrupoVeiculos;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloGrupoVeiculos;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPlanoCobranca;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Handlers;

public class CadastrarPlanoCobrancaCommandHandler(
    AppDbContext appDbContext,
    RepositorioPlanoCobrancaEmOrm repositorioPlano,
    RepositorioGrupoVeiculosEmOrm repositorioGrupo,
    ITenantProvider tenantProvider,
    IValidator<CadastrarPlanoCobrancaCommand> validator,
    ILogger<CadastrarPlanoCobrancaCommandHandler> logger
) : IRequestHandler<CadastrarPlanoCobrancaCommand, Result<CadastrarPlanoCobrancaResult>>
{
    public async Task<Result<CadastrarPlanoCobrancaResult>> Handle(
        CadastrarPlanoCobrancaCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        // O filtro por empresa garante que só grupos da própria locadora são encontrados.
        GrupoVeiculos? grupo = await repositorioGrupo.SelecionarPorIdAsync(command.GrupoVeiculosId);

        if (grupo is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.GrupoVeiculosId));

        if (await repositorioPlano.ExisteParaGrupoAsync(grupo.Id))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                "Já existe um plano de cobrança para este grupo de veículos."));

        try
        {
            PlanoCobranca plano = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                grupo.Id,
                command.Diario.PrecoDiaria,
                command.Diario.PrecoPorKm,
                command.Controlado.KmDisponiveis,
                command.Controlado.PrecoDiaria,
                command.Controlado.PrecoPorKmExtrapolado,
                command.Livre.PrecoDiaria
            );

            await repositorioPlano.CadastrarAsync(plano);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarPlanoCobrancaResult(plano.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante o cadastro de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarPlanoCobrancaCommandHandler(
    AppDbContext appDbContext,
    RepositorioPlanoCobrancaEmOrm repositorioPlano,
    ITenantProvider tenantProvider,
    IValidator<EditarPlanoCobrancaCommand> validator,
    ILogger<EditarPlanoCobrancaCommandHandler> logger
) : IRequestHandler<EditarPlanoCobrancaCommand, Result<EditarPlanoCobrancaResult>>
{
    public async Task<Result<EditarPlanoCobrancaResult>> Handle(
        EditarPlanoCobrancaCommand command, CancellationToken cancellationToken)
    {
        PlanoCobranca? plano = await repositorioPlano.SelecionarPorIdAsync(command.Id);

        if (plano is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        try
        {
            PlanoCobranca planoEditado = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                plano.GrupoVeiculosId,
                command.Diario.PrecoDiaria,
                command.Diario.PrecoPorKm,
                command.Controlado.KmDisponiveis,
                command.Controlado.PrecoDiaria,
                command.Controlado.PrecoPorKmExtrapolado,
                command.Livre.PrecoDiaria
            );

            plano.AtualizarRegistro(planoEditado);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarPlanoCobrancaResult(command.Diario, command.Controlado, command.Livre));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante a edição de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirPlanoCobrancaCommandHandler(
    AppDbContext appDbContext,
    RepositorioPlanoCobrancaEmOrm repositorioPlano,
    ILogger<ExcluirPlanoCobrancaCommandHandler> logger
) : IRequestHandler<ExcluirPlanoCobrancaCommand, Result<ExcluirPlanoCobrancaResult>>
{
    public async Task<Result<ExcluirPlanoCobrancaResult>> Handle(
        ExcluirPlanoCobrancaCommand command, CancellationToken cancellationToken)
    {
        PlanoCobranca? plano = await repositorioPlano.SelecionarPorIdAsync(command.Id);

        if (plano is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        // Quando existirem Automóveis e Aluguéis, bloquear aqui a exclusão de plano em uso.

        try
        {
            await repositorioPlano.ExcluirAsync(plano.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirPlanoCobrancaResult());
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante a exclusão de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarPlanoCobrancaPorIdQueryHandler(
    RepositorioPlanoCobrancaEmOrm repositorioPlano
) : IRequestHandler<SelecionarPlanoCobrancaPorIdQuery, Result<PlanoCobrancaDto>>
{
    public async Task<Result<PlanoCobrancaDto>> Handle(
        SelecionarPlanoCobrancaPorIdQuery query, CancellationToken cancellationToken)
    {
        PlanoCobranca? plano = await repositorioPlano.SelecionarPorIdAsync(query.Id);

        if (plano is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(plano.ParaDto());
    }
}

public class SelecionarPlanosCobrancaQueryHandler(
    RepositorioPlanoCobrancaEmOrm repositorioPlano
) : IRequestHandler<SelecionarPlanosCobrancaQuery, Result<SelecionarPlanosCobrancaResult>>
{
    public async Task<Result<SelecionarPlanosCobrancaResult>> Handle(
        SelecionarPlanosCobrancaQuery query, CancellationToken cancellationToken)
    {
        List<PlanoCobranca> planos = await repositorioPlano.SelecionarTodosAsync();

        return Result.Ok(new SelecionarPlanosCobrancaResult(
            planos.Select(p => p.ParaDto()).ToList()
        ));
    }
}
