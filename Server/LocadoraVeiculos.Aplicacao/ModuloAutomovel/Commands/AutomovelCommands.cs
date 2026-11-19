using FluentResults;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Commands;

public record AutomovelDto(
    Guid Id,
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId,
    string NomeGrupo,
    bool TemFoto,
    string? NomeArquivoFoto
);

public record CadastrarAutomovelCommand(
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId
) : IRequest<Result<CadastrarAutomovelResult>>;

public record CadastrarAutomovelResult(Guid Id);

public record EditarAutomovelCommand(
    Guid Id,
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId
) : IRequest<Result<EditarAutomovelResult>>;

public record EditarAutomovelResult(
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId
);

public record ExcluirAutomovelCommand(Guid Id) : IRequest<Result<ExcluirAutomovelResult>>;

public record ExcluirAutomovelResult();

public record SelecionarAutomovelPorIdQuery(Guid Id) : IRequest<Result<AutomovelDto>>;

public record SelecionarAutomoveisQuery(Guid? GrupoVeiculosId = null)
    : IRequest<Result<SelecionarAutomoveisResult>>;

public record SelecionarAutomoveisResult(IReadOnlyList<AutomovelDto> Registros);

public record AtualizarFotoAutomovelCommand(Guid Id, byte[] Conteudo, string NomeArquivo)
    : IRequest<Result<AtualizarFotoAutomovelResult>>;

public record AtualizarFotoAutomovelResult(string NomeArquivo);

public record RemoverFotoAutomovelCommand(Guid Id) : IRequest<Result<RemoverFotoAutomovelResult>>;

public record RemoverFotoAutomovelResult();

public record ObterFotoAutomovelQuery(Guid Id) : IRequest<Result<FotoAutomovelResult>>;

public record FotoAutomovelResult(byte[] Conteudo, string TipoConteudo);
