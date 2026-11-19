using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Commands;

public record CondutorDto(
    Guid Id,
    Guid ClienteId,
    string NomeCliente,
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Cnh,
    DateOnly ValidadeCnh,
    bool CnhVencida
);

public record CadastrarCondutorCommand(
    Guid ClienteId,
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Cnh,
    DateOnly ValidadeCnh
) : IRequest<Result<CadastrarCondutorResult>>;

public record CadastrarCondutorResult(Guid Id);

/// <summary>O cliente a quem o condutor pertence não muda na edição.</summary>
public record EditarCondutorCommand(
    Guid Id,
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Cnh,
    DateOnly ValidadeCnh
) : IRequest<Result<EditarCondutorResult>>;

public record EditarCondutorResult(CondutorDto Condutor);

public record ExcluirCondutorCommand(Guid Id) : IRequest<Result<ExcluirCondutorResult>>;

public record ExcluirCondutorResult();

public record SelecionarCondutorPorIdQuery(Guid Id) : IRequest<Result<CondutorDto>>;

public record SelecionarCondutoresQuery(Guid? ClienteId = null)
    : IRequest<Result<SelecionarCondutoresResult>>;

public record SelecionarCondutoresResult(IReadOnlyList<CondutorDto> Registros);
