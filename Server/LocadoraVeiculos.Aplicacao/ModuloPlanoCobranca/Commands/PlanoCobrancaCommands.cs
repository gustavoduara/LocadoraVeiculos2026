using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;

public record PlanoDiarioDto(decimal PrecoDiaria, decimal PrecoPorKm);

public record PlanoControladoDto(int KmDisponiveis, decimal PrecoDiaria, decimal PrecoPorKmExtrapolado);

public record PlanoLivreDto(decimal PrecoDiaria);

public record PlanoCobrancaDto(
    Guid Id,
    Guid GrupoVeiculosId,
    string NomeGrupo,
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);

public record CadastrarPlanoCobrancaCommand(
    Guid GrupoVeiculosId,
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
) : IRequest<Result<CadastrarPlanoCobrancaResult>>;

public record CadastrarPlanoCobrancaResult(Guid Id);

public record EditarPlanoCobrancaCommand(
    Guid Id,
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
) : IRequest<Result<EditarPlanoCobrancaResult>>;

public record EditarPlanoCobrancaResult(
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);

public record ExcluirPlanoCobrancaCommand(Guid Id) : IRequest<Result<ExcluirPlanoCobrancaResult>>;

public record ExcluirPlanoCobrancaResult();

public record SelecionarPlanoCobrancaPorIdQuery(Guid Id) : IRequest<Result<PlanoCobrancaDto>>;

public record SelecionarPlanosCobrancaQuery() : IRequest<Result<SelecionarPlanosCobrancaResult>>;

public record SelecionarPlanosCobrancaResult(IReadOnlyList<PlanoCobrancaDto> Registros);

/// <summary>Valores do plano exibidos na listagem de grupos de veículos.</summary>
public record PlanoCobrancaResumoDto(
    Guid Id,
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);
