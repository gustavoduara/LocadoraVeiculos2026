using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;

public record PlanoDiarioDto(decimal PrecoDiaria, decimal PrecoPorKm);

public record PlanoControladoDto(int KmDisponiveis, decimal PrecoDiaria, decimal PrecoPorKmExtrapolado);

public record PlanoLivreDto(decimal PrecoDiaria);

public record PlanoCobrancaDto(
    Guid Id,
    Guid AutomovelId,
    /// <summary>"Marca Modelo" do carro, para a lista mostrar de quem é o plano.</summary>
    string NomeAutomovel,
    string Placa,
    PlanoDiarioDto Diario,
    PlanoControladoDto Controlado,
    PlanoLivreDto Livre
);

public record CadastrarPlanoCobrancaCommand(
    Guid AutomovelId,
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

