using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Commands;

public record SelecionarConfiguracaoQuery() : IRequest<Result<SelecionarConfiguracaoResult>>;

public record SelecionarConfiguracaoResult(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool,
    decimal SeguroClienteDiaria,
    decimal SeguroTerceirosDiaria
);

public record EditarConfiguracaoCommand(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool,
    decimal SeguroClienteDiaria,
    decimal SeguroTerceirosDiaria
) : IRequest<Result<EditarConfiguracaoResult>>;

public record EditarConfiguracaoResult(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool,
    decimal SeguroClienteDiaria,
    decimal SeguroTerceirosDiaria
);
