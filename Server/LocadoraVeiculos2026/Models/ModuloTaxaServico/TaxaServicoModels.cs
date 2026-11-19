using LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Commands;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloTaxaServico;

public record CadastrarTaxaServicoRequest(string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca);

public record CadastrarTaxaServicoResponse(Guid Id);

public record EditarTaxaServicoRequest(string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca);

public record EditarTaxaServicoResponse(string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca);

public record SelecionarTaxasServicosResponse(IReadOnlyList<TaxaServicoDto> Registros);
