using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloParceiro;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCupom;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloParceiro;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Handlers;

internal static class ParceiroMapeamento
{
    public static ParceiroDto ParaDto(this Parceiro p) => new(p.Id, p.Nome);
}

public class CadastrarParceiroCommandHandler(
    AppDbContext appDbContext,
    RepositorioParceiroEmOrm repositorioParceiro,
    ITenantProvider tenantProvider,
    IValidator<CadastrarParceiroCommand> validator,
    ILogger<CadastrarParceiroCommandHandler> logger
) : IRequestHandler<CadastrarParceiroCommand, Result<CadastrarParceiroResult>>
{
    public async Task<Result<CadastrarParceiroResult>> Handle(
        CadastrarParceiroCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(resultadoValidacao.Errors.Select(e => e.ErrorMessage)));

        string nome = command.Nome.Trim();

        if (await repositorioParceiro.ExisteNomeAsync(nome))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um parceiro com este nome."));

        try
        {
            Parceiro parceiro = new(tenantProvider.EmpresaId.GetValueOrDefault(), nome);

            await repositorioParceiro.CadastrarAsync(parceiro);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarParceiroResult(parceiro.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarParceiroCommandHandler(
    AppDbContext appDbContext,
    RepositorioParceiroEmOrm repositorioParceiro,
    ITenantProvider tenantProvider,
    IValidator<EditarParceiroCommand> validator,
    ILogger<EditarParceiroCommandHandler> logger
) : IRequestHandler<EditarParceiroCommand, Result<EditarParceiroResult>>
{
    public async Task<Result<EditarParceiroResult>> Handle(
        EditarParceiroCommand command, CancellationToken cancellationToken)
    {
        Parceiro? parceiro = await repositorioParceiro.SelecionarPorIdAsync(command.Id);

        if (parceiro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(resultadoValidacao.Errors.Select(e => e.ErrorMessage)));

        string nome = command.Nome.Trim();

        if (await repositorioParceiro.ExisteNomeAsync(nome, ignorarId: parceiro.Id))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um parceiro com este nome."));

        try
        {
            parceiro.AtualizarRegistro(new Parceiro(tenantProvider.EmpresaId.GetValueOrDefault(), nome));

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarParceiroResult(nome));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirParceiroCommandHandler(
    AppDbContext appDbContext,
    RepositorioParceiroEmOrm repositorioParceiro,
    RepositorioCupomEmOrm repositorioCupom,
    ILogger<ExcluirParceiroCommandHandler> logger
) : IRequestHandler<ExcluirParceiroCommand, Result<ExcluirParceiroResult>>
{
    public async Task<Result<ExcluirParceiroResult>> Handle(
        ExcluirParceiroCommand command, CancellationToken cancellationToken)
    {
        Parceiro? parceiro = await repositorioParceiro.SelecionarPorIdAsync(command.Id);

        if (parceiro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (await repositorioCupom.ExisteParaParceiroAsync(parceiro.Id))
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir um parceiro vinculado a um cupom."));

        try
        {
            await repositorioParceiro.ExcluirAsync(parceiro.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirParceiroResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarParceiroPorIdQueryHandler(
    RepositorioParceiroEmOrm repositorioParceiro
) : IRequestHandler<SelecionarParceiroPorIdQuery, Result<ParceiroDto>>
{
    public async Task<Result<ParceiroDto>> Handle(
        SelecionarParceiroPorIdQuery query, CancellationToken cancellationToken)
    {
        Parceiro? parceiro = await repositorioParceiro.SelecionarPorIdAsync(query.Id);

        if (parceiro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(parceiro.ParaDto());
    }
}

public class SelecionarParceirosQueryHandler(
    RepositorioParceiroEmOrm repositorioParceiro
) : IRequestHandler<SelecionarParceirosQuery, Result<SelecionarParceirosResult>>
{
    public async Task<Result<SelecionarParceirosResult>> Handle(
        SelecionarParceirosQuery query, CancellationToken cancellationToken)
    {
        List<Parceiro> parceiros = await repositorioParceiro.SelecionarTodosAsync();

        return Result.Ok(new SelecionarParceirosResult(parceiros.Select(p => p.ParaDto()).ToList()));
    }
}
