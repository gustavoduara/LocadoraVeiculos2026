using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloPlanoCobranca;

public record CadastrarPlanoCobrancaRequest(
    Guid AutomovelId,
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);

public record CadastrarPlanoCobrancaResponse(Guid Id);

public record EditarPlanoCobrancaRequest(
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);

public record EditarPlanoCobrancaResponse(
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);

public record SelecionarPlanosCobrancaResponse(IReadOnlyList<PlanoCobrancaDto> Registros);
