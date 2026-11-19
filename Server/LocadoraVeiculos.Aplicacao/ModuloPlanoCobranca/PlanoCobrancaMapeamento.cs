using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca;

internal static class PlanoCobrancaMapeamento
{
    public static PlanoCobrancaDto ParaDto(this PlanoCobranca plano) => new(
        plano.Id,
        plano.GrupoVeiculosId,
        plano.GrupoVeiculos?.Nome ?? string.Empty,
        new PlanoDiarioDto(plano.DiarioPrecoDiaria, plano.DiarioPrecoPorKm),
        new PlanoControladoDto(
            plano.ControladoKmDisponiveis,
            plano.ControladoPrecoDiaria,
            plano.ControladoPrecoPorKmExtrapolado
        ),
        new PlanoLivreDto(plano.LivrePrecoDiaria)
    );
}
