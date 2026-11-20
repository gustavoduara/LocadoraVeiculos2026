using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloCupom;

/// <summary>Data de validade no formato ISO (yyyy-MM-dd).</summary>
public record CadastrarCupomRequest(string Nome, decimal Valor, DateOnly DataValidade, Guid ParceiroId);

public record CadastrarCupomResponse(Guid Id);

public record EditarCupomRequest(string Nome, decimal Valor, DateOnly DataValidade, Guid ParceiroId);

public record SelecionarCuponsResponse(IReadOnlyList<CupomDto> Registros);
