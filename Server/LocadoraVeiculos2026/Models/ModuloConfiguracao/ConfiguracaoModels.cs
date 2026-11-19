namespace LocadoraDeVeiculos.WebApi.Models.ModuloConfiguracao;

public record EditarConfiguracaoRequest(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool
);

public record ConfiguracaoResponse(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool
);
