using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Commands;

public record AlterarStatusFuncionarioCommand(Guid Id, bool Ativo)
    : IRequest<Result<AlterarStatusFuncionarioResult>>;

public record AlterarStatusFuncionarioResult(Guid Id, bool Ativo);
