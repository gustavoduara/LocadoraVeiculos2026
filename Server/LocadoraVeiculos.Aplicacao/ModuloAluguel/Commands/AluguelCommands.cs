using FluentResults;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;

/// <summary>Dados informados ao registrar (ou editar/simular) um aluguel.</summary>
public record DadosAluguelDto(
    Guid CondutorId,
    Guid AutomovelId,
    TipoPlano TipoPlano,
    DateOnly DataSaida,
    DateOnly DataRetornoPrevista,
    int KmInicial,
    TipoSeguro TipoSeguro,
    IReadOnlyList<Guid> TaxasIds,
    Guid? CupomId = null
);

public record AluguelTaxaDto(Guid TaxaServicoId, string Nome, decimal PrecoUnitario, TipoCobrancaTaxa TipoCobranca, decimal Valor);

public record DevolucaoDto(DateOnly DataDevolucao, int KmFinal, bool TanqueCheio, DetalhamentoFinal Final);

public record AluguelDto(
    Guid Id,
    Guid CondutorId,
    string NomeCondutor,
    Guid ClienteId,
    string NomeCliente,
    Guid AutomovelId,
    string DescricaoVeiculo,
    string NomeGrupo,
    TipoPlano TipoPlano,
    DateOnly DataSaida,
    DateOnly DataRetornoPrevista,
    int KmInicial,
    TipoSeguro TipoSeguro,
    IReadOnlyList<AluguelTaxaDto> Taxas,
    StatusAluguel Status,
    DetalhamentoPrevisto Previsto,
    DevolucaoDto? Devolucao,
    Guid? CupomId = null,
    string? NomeCupom = null
);

/// <summary>Resultado de uma simulação de preço (nada é gravado).</summary>
public record SimulacaoAluguelDto(IReadOnlyList<AluguelTaxaDto> Taxas, DetalhamentoPrevisto Previsto);

public record CadastrarAluguelCommand(DadosAluguelDto Dados) : IRequest<Result<CadastrarAluguelResult>>;

public record CadastrarAluguelResult(Guid Id);

public record EditarAluguelCommand(Guid Id, DadosAluguelDto Dados) : IRequest<Result<EditarAluguelResult>>;

public record EditarAluguelResult(AluguelDto Aluguel);

public record ExcluirAluguelCommand(Guid Id) : IRequest<Result<ExcluirAluguelResult>>;

public record ExcluirAluguelResult();

public record SelecionarAluguelPorIdQuery(Guid Id) : IRequest<Result<AluguelDto>>;

public record SelecionarAlugueisQuery(StatusAluguel? Status = null) : IRequest<Result<SelecionarAlugueisResult>>;

public record SelecionarAlugueisResult(IReadOnlyList<AluguelDto> Registros);

public record CalcularAluguelQuery(DadosAluguelDto Dados) : IRequest<Result<SimulacaoAluguelDto>>;

public record DevolverAluguelCommand(Guid Id, DateOnly DataDevolucao, int KmFinal, bool TanqueCheio)
    : IRequest<Result<DevolverAluguelResult>>;

public record DevolverAluguelResult(AluguelDto Aluguel);

public record CalcularDevolucaoQuery(Guid Id, DateOnly DataDevolucao, int KmFinal, bool TanqueCheio)
    : IRequest<Result<DetalhamentoFinal>>;
