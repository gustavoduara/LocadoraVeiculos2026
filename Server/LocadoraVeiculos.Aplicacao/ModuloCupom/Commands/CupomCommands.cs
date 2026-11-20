using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;

public record CupomDto(
    Guid Id,
    string Nome,
    decimal Valor,
    DateOnly DataValidade,
    Guid ParceiroId,
    string NomeParceiro,
    bool Ativo
);

public record CadastrarCupomCommand(string Nome, decimal Valor, DateOnly DataValidade, Guid ParceiroId)
    : IRequest<Result<CadastrarCupomResult>>;

public record CadastrarCupomResult(Guid Id);

public record EditarCupomCommand(Guid Id, string Nome, decimal Valor, DateOnly DataValidade, Guid ParceiroId)
    : IRequest<Result<EditarCupomResult>>;

public record EditarCupomResult(CupomDto Cupom);

public record ExcluirCupomCommand(Guid Id) : IRequest<Result<ExcluirCupomResult>>;

public record ExcluirCupomResult();

public record SelecionarCupomPorIdQuery(Guid Id) : IRequest<Result<CupomDto>>;

public record SelecionarCuponsQuery() : IRequest<Result<SelecionarCuponsResult>>;

public record SelecionarCuponsResult(IReadOnlyList<CupomDto> Registros);

/// <summary>Quantas vezes um cupom foi usado em aluguéis.</summary>
public record CupomMaisUtilizadoDto(Guid CupomId, string Nome, string NomeParceiro, decimal Valor, int Usos);

/// <summary>Quantas vezes os cupons de um parceiro foram usados (para decidir em quais parceiros investir).</summary>
public record ParceiroMaisUtilizadoDto(Guid ParceiroId, string NomeParceiro, int Usos);

public record CuponsMaisUtilizadosQuery() : IRequest<Result<CuponsMaisUtilizadosResult>>;

public record CuponsMaisUtilizadosResult(
    IReadOnlyList<CupomMaisUtilizadoDto> Cupons,
    IReadOnlyList<ParceiroMaisUtilizadoDto> Parceiros
);
