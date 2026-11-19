using LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Commands;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloCondutor;

/// <summary><c>ValidadeCnh</c> no formato ISO (yyyy-MM-dd).</summary>
public record CadastrarCondutorRequest(
    Guid ClienteId,
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Cnh,
    DateOnly ValidadeCnh
);

public record CadastrarCondutorResponse(Guid Id);

public record EditarCondutorRequest(
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Cnh,
    DateOnly ValidadeCnh
);

public record SelecionarCondutoresResponse(IReadOnlyList<CondutorDto> Registros);
