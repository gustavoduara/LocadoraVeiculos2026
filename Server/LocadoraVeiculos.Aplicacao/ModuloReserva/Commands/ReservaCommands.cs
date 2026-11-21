using FluentResults;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloReserva.Commands;

/// <summary>
/// O que o cliente escolhe ao reservar. Datas em yyyy-MM-dd. <c>TaxasIds</c> são taxas e serviços do catálogo da locadora
/// do carro; <c>Cupom</c> é o nome do cupom (opcional) dessa locadora.
/// </summary>
public record DadosReservaDto(
    Guid AutomovelId,
    DateOnly Retirada,
    DateOnly Devolucao,
    TipoPlano TipoPlano,
    TipoSeguro TipoSeguro,
    IReadOnlyList<Guid>? TaxasIds = null,
    string? Cupom = null
);

/// <summary>Uma taxa ou serviço da reserva, com o valor já multiplicado pelos dias quando a cobrança é diária.</summary>
public record TaxaDaReservaDto(Guid TaxaServicoId, string Nome, decimal PrecoUnitario, TipoCobrancaTaxa TipoCobranca, decimal Valor);

public record EstimativaReservaDto(
    int Dias,
    decimal ValorDiarias,
    decimal ValorSeguro,
    decimal ValorCaucao,
    decimal ValorTotalPrevisto,
    decimal ValorTaxas = 0m,
    decimal ValorDesconto = 0m,
    IReadOnlyList<TaxaDaReservaDto>? Taxas = null,
    string? CupomNome = null,
    TipoDesconto? CupomTipo = null,
    decimal CupomValor = 0m
);

public record ReservaDto(
    Guid Id,
    StatusReserva Status,
    string NomeLocadora,
    /// <summary>Só aparece depois que a reserva é aprovada.</summary>
    string? TelefoneLocadora,
    Guid AutomovelId,
    string DescricaoVeiculo,
    string NomeGrupo,
    DateOnly Retirada,
    DateOnly Devolucao,
    int Dias,
    TipoPlano TipoPlano,
    TipoSeguro TipoSeguro,
    decimal ValorDiarias,
    decimal ValorSeguro,
    decimal ValorCaucao,
    decimal ValorTotalPrevisto,
    string? MotivoRecusa,
    Guid? AluguelId,
    DateTimeOffset CriadaEmUtc,
    DateTimeOffset? RespondidaEmUtc,
    string ClienteNome,
    string ClienteEmail,
    string ClienteTelefone,
    string ClienteCpf,
    string ClienteCnh,
    DateOnly ClienteValidadeCnh,
    string ClienteCidade,
    string ClienteEstado,
    decimal ValorTaxas = 0m,
    decimal ValorDesconto = 0m,
    IReadOnlyList<TaxaDaReservaDto>? Taxas = null,
    string? CupomNome = null,
    TipoDesconto? CupomTipo = null,
    decimal CupomValor = 0m
);

// ---------------- lado do cliente ----------------

public record CalcularReservaQuery(DadosReservaDto Dados) : IRequest<Result<EstimativaReservaDto>>;

public record CriarReservaCommand(DadosReservaDto Dados) : IRequest<Result<CriarReservaResult>>;

public record CriarReservaResult(Guid Id);

public record SelecionarMinhasReservasQuery() : IRequest<Result<SelecionarReservasResult>>;

public record CancelarMinhaReservaCommand(Guid Id) : IRequest<Result<ReservaDto>>;

// ---------------- lado da locadora ----------------

public record SelecionarReservasRecebidasQuery(StatusReserva? Status) : IRequest<Result<SelecionarReservasResult>>;

public record SelecionarReservasResult(IReadOnlyList<ReservaDto> Registros);

/// <summary>Aprova o pedido: cria (ou reaproveita) o cliente e o condutor na locadora e abre o aluguel.</summary>
public record AprovarReservaCommand(Guid Id, int KmInicial) : IRequest<Result<ReservaDto>>;

public record RecusarReservaCommand(Guid Id, string? Motivo) : IRequest<Result<ReservaDto>>;

public record ContarReservasPendentesQuery() : IRequest<Result<int>>;
