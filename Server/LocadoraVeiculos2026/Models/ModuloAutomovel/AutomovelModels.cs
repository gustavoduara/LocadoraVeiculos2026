using LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloAutomovel;

public record CadastrarAutomovelRequest(
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId
);

public record CadastrarAutomovelResponse(Guid Id);

public record EditarAutomovelRequest(
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId
);

public record EditarAutomovelResponse(
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId
);

public record SelecionarAutomoveisResponse(IReadOnlyList<AutomovelDto> Registros);

public record EnviarFotoAutomovelResponse(string NomeArquivo);
