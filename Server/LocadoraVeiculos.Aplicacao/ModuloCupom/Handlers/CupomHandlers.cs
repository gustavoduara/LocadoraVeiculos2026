using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloCupom;
using LocadoraDeVeiculos.Dominio.ModuloParceiro;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCupom;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloParceiro;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCupom.Handlers;

internal static class CupomMapeamento
{
    public static CupomDto ParaDto(this Cupom c) => new(
        c.Id,
        c.Nome,
        c.Valor,
        c.Tipo,
        c.DataValidade,
        c.ParceiroId,
        c.Parceiro?.Nome ?? string.Empty,
        c.EstaAtivo(DateOnly.FromDateTime(DateTime.UtcNow))
    );
}

public class CadastrarCupomCommandHandler(
    AppDbContext appDbContext,
    RepositorioCupomEmOrm repositorioCupom,
    RepositorioParceiroEmOrm repositorioParceiro,
    ITenantProvider tenantProvider,
    IValidator<CadastrarCupomCommand> validator,
    ILogger<CadastrarCupomCommandHandler> logger
) : IRequestHandler<CadastrarCupomCommand, Result<CadastrarCupomResult>>
{
    public async Task<Result<CadastrarCupomResult>> Handle(
        CadastrarCupomCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(resultadoValidacao.Errors.Select(e => e.ErrorMessage)));

        // O filtro por empresa garante que só parceiros da própria locadora são encontrados.
        Parceiro? parceiro = await repositorioParceiro.SelecionarPorIdAsync(command.ParceiroId);

        if (parceiro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.ParceiroId));

        string nome = command.Nome.Trim();

        if (await repositorioCupom.ExisteNomeAsync(nome))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um cupom com este nome."));

        try
        {
            Cupom cupom = new(
                tenantProvider.EmpresaId.GetValueOrDefault(), nome, command.Valor, command.DataValidade, parceiro.Id, command.Tipo);

            await repositorioCupom.CadastrarAsync(cupom);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarCupomResult(cupom.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarCupomCommandHandler(
    AppDbContext appDbContext,
    RepositorioCupomEmOrm repositorioCupom,
    RepositorioParceiroEmOrm repositorioParceiro,
    ITenantProvider tenantProvider,
    IValidator<EditarCupomCommand> validator,
    ILogger<EditarCupomCommandHandler> logger
) : IRequestHandler<EditarCupomCommand, Result<EditarCupomResult>>
{
    public async Task<Result<EditarCupomResult>> Handle(
        EditarCupomCommand command, CancellationToken cancellationToken)
    {
        Cupom? cupom = await repositorioCupom.SelecionarPorIdAsync(command.Id);

        if (cupom is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(resultadoValidacao.Errors.Select(e => e.ErrorMessage)));

        Parceiro? parceiro = await repositorioParceiro.SelecionarPorIdAsync(command.ParceiroId);

        if (parceiro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.ParceiroId));

        string nome = command.Nome.Trim();

        if (await repositorioCupom.ExisteNomeAsync(nome, ignorarId: cupom.Id))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um cupom com este nome."));

        try
        {
            // Aluguéis já feitos guardam uma cópia do valor, então editar o cupom não muda o que já foi cobrado.
            cupom.AtualizarRegistro(new Cupom(
                tenantProvider.EmpresaId.GetValueOrDefault(), nome, command.Valor, command.DataValidade, parceiro.Id, command.Tipo));

            cupom.Parceiro = parceiro;

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarCupomResult(cupom.ParaDto()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirCupomCommandHandler(
    AppDbContext appDbContext,
    RepositorioCupomEmOrm repositorioCupom,
    RepositorioAluguelEmOrm repositorioAluguel,
    ILogger<ExcluirCupomCommandHandler> logger
) : IRequestHandler<ExcluirCupomCommand, Result<ExcluirCupomResult>>
{
    public async Task<Result<ExcluirCupomResult>> Handle(
        ExcluirCupomCommand command, CancellationToken cancellationToken)
    {
        Cupom? cupom = await repositorioCupom.SelecionarPorIdAsync(command.Id);

        if (cupom is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (await repositorioAluguel.ExisteEmAbertoParaCupomAsync(cupom.Id))
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir um cupom usado em um aluguel ainda não concluído."));

        try
        {
            await repositorioCupom.ExcluirAsync(cupom.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirCupomResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarCupomPorIdQueryHandler(
    RepositorioCupomEmOrm repositorioCupom
) : IRequestHandler<SelecionarCupomPorIdQuery, Result<CupomDto>>
{
    public async Task<Result<CupomDto>> Handle(
        SelecionarCupomPorIdQuery query, CancellationToken cancellationToken)
    {
        Cupom? cupom = await repositorioCupom.SelecionarPorIdAsync(query.Id);

        if (cupom is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(cupom.ParaDto());
    }
}

public class SelecionarCuponsQueryHandler(
    RepositorioCupomEmOrm repositorioCupom
) : IRequestHandler<SelecionarCuponsQuery, Result<SelecionarCuponsResult>>
{
    public async Task<Result<SelecionarCuponsResult>> Handle(
        SelecionarCuponsQuery query, CancellationToken cancellationToken)
    {
        List<Cupom> cupons = await repositorioCupom.SelecionarTodosAsync();

        return Result.Ok(new SelecionarCuponsResult(cupons.Select(c => c.ParaDto()).ToList()));
    }
}

/// <summary>Ranking que o proprietário usa para investir nos parceiros que geram mais retorno.</summary>
public class CuponsMaisUtilizadosQueryHandler(
    RepositorioAluguelEmOrm repositorioAluguel,
    RepositorioCupomEmOrm repositorioCupom
) : IRequestHandler<CuponsMaisUtilizadosQuery, Result<CuponsMaisUtilizadosResult>>
{
    public async Task<Result<CuponsMaisUtilizadosResult>> Handle(
        CuponsMaisUtilizadosQuery query, CancellationToken cancellationToken)
    {
        List<UsoDeCupom> usos = await repositorioAluguel.SelecionarUsosDeCuponsAsync();

        List<Cupom> cupons = await repositorioCupom.SelecionarPorIdsAsync(usos.Select(u => u.CupomId).Distinct().ToList());

        var cuponsPorId = cupons.ToDictionary(c => c.Id);

        List<CupomMaisUtilizadoDto> ranking = usos
            .GroupBy(u => u.CupomId)
            .Select(g =>
            {
                cuponsPorId.TryGetValue(g.Key, out Cupom? cupom);

                // O nome e o valor do aluguel valem se o cupom já foi excluído.
                return new CupomMaisUtilizadoDto(
                    g.Key,
                    cupom?.Nome ?? g.First().NomeCupom,
                    cupom?.Parceiro?.Nome ?? string.Empty,
                    cupom?.Valor ?? g.First().ValorCupom,
                    g.Count(),
                    cupom?.Tipo ?? g.First().TipoCupom);
            })
            .OrderByDescending(c => c.Usos)
            .ThenBy(c => c.Nome)
            .ToList();

        List<ParceiroMaisUtilizadoDto> parceiros = usos
            .Where(u => cuponsPorId.ContainsKey(u.CupomId))
            .GroupBy(u => cuponsPorId[u.CupomId].ParceiroId)
            .Select(g => new ParceiroMaisUtilizadoDto(
                g.Key,
                cuponsPorId[g.First().CupomId].Parceiro?.Nome ?? string.Empty,
                g.Count()))
            .OrderByDescending(p => p.Usos)
            .ThenBy(p => p.NomeParceiro)
            .ToList();

        return Result.Ok(new CuponsMaisUtilizadosResult(ranking, parceiros));
    }
}
