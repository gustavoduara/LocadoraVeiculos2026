using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloTaxaServico;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Handlers;

internal static class TaxaServicoMapeamento
{
    public static TaxaServicoDto ParaDto(this TaxaServico t) => new(t.Id, t.Nome, t.Preco, t.TipoCobranca);
}

public class CadastrarTaxaServicoCommandHandler(
    AppDbContext appDbContext,
    RepositorioTaxaServicoEmOrm repositorioTaxa,
    ITenantProvider tenantProvider,
    IValidator<CadastrarTaxaServicoCommand> validator,
    ILogger<CadastrarTaxaServicoCommandHandler> logger
) : IRequestHandler<CadastrarTaxaServicoCommand, Result<CadastrarTaxaServicoResult>>
{
    public async Task<Result<CadastrarTaxaServicoResult>> Handle(
        CadastrarTaxaServicoCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        string nome = command.Nome.Trim();

        if (await repositorioTaxa.ExisteNomeAsync(nome))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe uma taxa ou serviço com este nome."));

        try
        {
            TaxaServico taxa = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                nome,
                command.Preco,
                command.TipoCobranca
            );

            await repositorioTaxa.CadastrarAsync(taxa);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarTaxaServicoResult(taxa.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarTaxaServicoCommandHandler(
    AppDbContext appDbContext,
    RepositorioTaxaServicoEmOrm repositorioTaxa,
    ITenantProvider tenantProvider,
    RepositorioAluguelEmOrm repositorioAluguel,
    IValidator<EditarTaxaServicoCommand> validator,
    ILogger<EditarTaxaServicoCommandHandler> logger
) : IRequestHandler<EditarTaxaServicoCommand, Result<EditarTaxaServicoResult>>
{
    public async Task<Result<EditarTaxaServicoResult>> Handle(
        EditarTaxaServicoCommand command, CancellationToken cancellationToken)
    {
        TaxaServico? taxa = await repositorioTaxa.SelecionarPorIdAsync(command.Id);

        if (taxa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        string nome = command.Nome.Trim();

        if (await repositorioTaxa.ExisteNomeAsync(nome, ignorarId: taxa.Id))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe uma taxa ou serviço com este nome."));

        if (await repositorioAluguel.ExisteEmAbertoParaTaxaAsync(taxa.Id))
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(
                "Não é possível editar uma taxa ou serviço usado em um aluguel ainda não concluído."));


        try
        {
            TaxaServico taxaEditada = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                nome,
                command.Preco,
                command.TipoCobranca
            );

            taxa.AtualizarRegistro(taxaEditada);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarTaxaServicoResult(nome, command.Preco, command.TipoCobranca));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirTaxaServicoCommandHandler(
    AppDbContext appDbContext,
    RepositorioTaxaServicoEmOrm repositorioTaxa,
    RepositorioAluguelEmOrm repositorioAluguel,
    ILogger<ExcluirTaxaServicoCommandHandler> logger
) : IRequestHandler<ExcluirTaxaServicoCommand, Result<ExcluirTaxaServicoResult>>
{
    public async Task<Result<ExcluirTaxaServicoResult>> Handle(
        ExcluirTaxaServicoCommand command, CancellationToken cancellationToken)
    {
        TaxaServico? taxa = await repositorioTaxa.SelecionarPorIdAsync(command.Id);

        if (taxa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (await repositorioAluguel.ExisteEmAbertoParaTaxaAsync(taxa.Id))
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir uma taxa ou serviço usado em um aluguel ainda não concluído."));


        try
        {
            await repositorioTaxa.ExcluirAsync(taxa.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirTaxaServicoResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarTaxaServicoPorIdQueryHandler(
    RepositorioTaxaServicoEmOrm repositorioTaxa
) : IRequestHandler<SelecionarTaxaServicoPorIdQuery, Result<TaxaServicoDto>>
{
    public async Task<Result<TaxaServicoDto>> Handle(
        SelecionarTaxaServicoPorIdQuery query, CancellationToken cancellationToken)
    {
        TaxaServico? taxa = await repositorioTaxa.SelecionarPorIdAsync(query.Id);

        if (taxa is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(taxa.ParaDto());
    }
}

public class SelecionarTaxasServicosQueryHandler(
    RepositorioTaxaServicoEmOrm repositorioTaxa
) : IRequestHandler<SelecionarTaxasServicosQuery, Result<SelecionarTaxasServicosResult>>
{
    public async Task<Result<SelecionarTaxasServicosResult>> Handle(
        SelecionarTaxasServicosQuery query, CancellationToken cancellationToken)
    {
        List<TaxaServico> taxas = await repositorioTaxa.SelecionarTodosAsync();

        return Result.Ok(new SelecionarTaxasServicosResult(taxas.Select(t => t.ParaDto()).ToList()));
    }
}
