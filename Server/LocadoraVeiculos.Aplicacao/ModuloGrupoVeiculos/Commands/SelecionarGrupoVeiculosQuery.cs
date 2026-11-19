using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloGrupoVeiculos.Commands;

public record SelecionarGruposVeiculosQuery()
    : IRequest<Result<SelecionarGruposVeiculosResult>>;

public record SelecionarGruposVeiculosResult(IReadOnlyList<SelecionarGruposVeiculosDto> Registros);

public record SelecionarGruposVeiculosDto(Guid Id, string Nome, PlanoCobrancaResumoDto? Plano = null);