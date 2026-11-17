using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Commands;

public record SelecionarMeuPerfilQuery() : IRequest<Result<SelecionarMeuPerfilResult>>;

public record SelecionarMeuPerfilResult(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    string Email,
    decimal Salario,
    DateTimeOffset AdmissaoEmUtc
);

public record EditarMeuPerfilCommand(string NomeCompleto, string Cpf)
    : IRequest<Result<EditarMeuPerfilResult>>;

public record EditarMeuPerfilResult(string NomeCompleto, string Cpf);
