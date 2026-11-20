namespace LocadoraDeVeiculos.WebApi.Models.ModuloConfiguracao;

public record EditarConfiguracaoRequest(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool,
    decimal SeguroClienteDiaria,
    decimal SeguroTerceirosDiaria
);

public record EditarDadosDaLocadoraRequest(string? NomeFantasia, string? Telefone);

public record ConfiguracaoResponse(
    decimal PrecoGasolina,
    decimal PrecoGas,
    decimal PrecoDiesel,
    decimal PrecoAlcool,
    decimal SeguroClienteDiaria,
    decimal SeguroTerceirosDiaria
);
