using FluentResults;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Commands;

public record TaxaServicoDto(Guid Id, string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca);

public record CadastrarTaxaServicoCommand(string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca)
    : IRequest<Result<CadastrarTaxaServicoResult>>;

public record CadastrarTaxaServicoResult(Guid Id);

public record EditarTaxaServicoCommand(Guid Id, string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca)
    : IRequest<Result<EditarTaxaServicoResult>>;

public record EditarTaxaServicoResult(string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca);

public record ExcluirTaxaServicoCommand(Guid Id) : IRequest<Result<ExcluirTaxaServicoResult>>;

public record ExcluirTaxaServicoResult();

public record SelecionarTaxaServicoPorIdQuery(Guid Id) : IRequest<Result<TaxaServicoDto>>;

public record SelecionarTaxasServicosQuery() : IRequest<Result<SelecionarTaxasServicosResult>>;

public record SelecionarTaxasServicosResult(IReadOnlyList<TaxaServicoDto> Registros);
