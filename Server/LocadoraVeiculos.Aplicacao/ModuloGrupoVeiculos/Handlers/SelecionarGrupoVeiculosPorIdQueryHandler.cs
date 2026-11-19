using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloGrupoVeiculos.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloGrupoVeiculos;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPlanoCobranca;
using MediatR;
using System.Collections.Immutable;

namespace LocadoraDeVeiculos.Aplicacao.ModuloGrupoVeiculos.Handlers;

public class SelecionarGruposVeiculosQueryHandler(
    RepositorioGrupoVeiculosEmOrm repositorioGrupoVeiculos,
    RepositorioPlanoCobrancaEmOrm repositorioPlanoCobranca
) : IRequestHandler<SelecionarGruposVeiculosQuery, Result<SelecionarGruposVeiculosResult>>
{
    public async Task<Result<SelecionarGruposVeiculosResult>> Handle(
        SelecionarGruposVeiculosQuery request, CancellationToken cancellationToken)
    {
        var registros = await repositorioGrupoVeiculos
            .SelecionarTodosAsync();

        // Cada grupo exibe os valores do seu plano de cobrança (quando já cadastrado).
        Dictionary<Guid, PlanoCobranca> planosPorGrupo =
            (await repositorioPlanoCobranca.SelecionarTodosAsync())
                .ToDictionary(p => p.GrupoVeiculosId);

        var dtos = registros
            .Select(r => new SelecionarGruposVeiculosDto(
                r.Id,
                r.Nome,
                planosPorGrupo.TryGetValue(r.Id, out PlanoCobranca? plano)
                    ? new PlanoCobrancaResumoDto(
                        plano.Id,
                        new PlanoDiarioDto(plano.DiarioPrecoDiaria, plano.DiarioPrecoPorKm),
                        new PlanoControladoDto(
                            plano.ControladoKmDisponiveis,
                            plano.ControladoPrecoDiaria,
                            plano.ControladoPrecoPorKmExtrapolado
                        ),
                        new PlanoLivreDto(plano.LivrePrecoDiaria)
                    )
                    : null
            ))
            .ToImmutableList();

        var response = new SelecionarGruposVeiculosResult(dtos);

        return Result.Ok(response);
    }
}
