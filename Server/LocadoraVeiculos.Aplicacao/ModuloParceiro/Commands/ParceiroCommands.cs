using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Commands;

public record ParceiroDto(Guid Id, string Nome);

public record CadastrarParceiroCommand(string Nome) : IRequest<Result<CadastrarParceiroResult>>;

public record CadastrarParceiroResult(Guid Id);

public record EditarParceiroCommand(Guid Id, string Nome) : IRequest<Result<EditarParceiroResult>>;

public record EditarParceiroResult(string Nome);

public record ExcluirParceiroCommand(Guid Id) : IRequest<Result<ExcluirParceiroResult>>;

public record ExcluirParceiroResult();

public record SelecionarParceiroPorIdQuery(Guid Id) : IRequest<Result<ParceiroDto>>;

public record SelecionarParceirosQuery() : IRequest<Result<SelecionarParceirosResult>>;

public record SelecionarParceirosResult(IReadOnlyList<ParceiroDto> Registros);
