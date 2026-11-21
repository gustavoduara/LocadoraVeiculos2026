using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloCupom;

/// <summary>Data de validade no formato ISO (yyyy-MM-dd). Tipo: Valor (reais) ou Percentual (0 a 100).</summary>
public record CadastrarCupomRequest(
    string Nome, decimal Valor, DateOnly DataValidade, Guid ParceiroId, TipoDesconto Tipo = TipoDesconto.Valor);

public record CadastrarCupomResponse(Guid Id);

public record EditarCupomRequest(
    string Nome, decimal Valor, DateOnly DataValidade, Guid ParceiroId, TipoDesconto Tipo = TipoDesconto.Valor);

public record SelecionarCuponsResponse(IReadOnlyList<CupomDto> Registros);
