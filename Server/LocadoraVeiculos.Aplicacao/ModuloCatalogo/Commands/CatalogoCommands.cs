using FluentResults;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCatalogo.Commands;

public enum OrdenacaoCatalogo
{
    MenorPreco,
    MaiorPreco,
    MaisNovo,
    Marca
}

/// <summary>Tudo é opcional: sem filtros, devolve a frota inteira de todas as locadoras.</summary>
public record PesquisarCatalogoQuery(
    DateOnly? Retirada,
    DateOnly? Devolucao,
    string? Texto,
    string? Categoria,
    TipoCombustivel? Combustivel,
    int? AnoMinimo,
    decimal? DiariaMaxima,
    Guid? LocadoraId,
    bool ApenasDisponiveis,
    OrdenacaoCatalogo Ordenacao,
    int Pagina,
    int TamanhoPagina
) : IRequest<Result<CatalogoResult>>;

public record CarroCatalogoDto(
    Guid Id,
    Guid LocadoraId,
    string Locadora,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel Combustivel,
    string Categoria,
    bool TemFoto,
    /// <summary>Total de fotos do carro (capa + extras): o carrossel mostra de 0 a QuantidadeFotos − 1.</summary>
    int QuantidadeFotos,
    decimal? DiariaAPartirDe,
    /// <summary>Diárias do período × menor diária (sem km, taxas, seguro e caução). Só com datas.</summary>
    decimal? TotalDoPeriodo,
    /// <summary>Só é informado quando a pesquisa tem datas.</summary>
    bool? Disponivel
);

public record CatalogoResult(
    IReadOnlyList<CarroCatalogoDto> Registros,
    int Total,
    int Pagina,
    int TamanhoPagina,
    int? Dias
);

public record FiltrosDoCatalogoQuery() : IRequest<Result<FiltrosDoCatalogoResult>>;

public record LocadoraFiltroDto(Guid Id, string Nome);

public record FiltrosDoCatalogoResult(
    IReadOnlyList<string> Categorias,
    IReadOnlyList<string> Marcas,
    IReadOnlyList<LocadoraFiltroDto> Locadoras
);

public record FotoDoCatalogoQuery(Guid AutomovelId, int Posicao = 0) : IRequest<Result<FotoDoCatalogoResult>>;

public record FotoDoCatalogoResult(byte[] Conteudo, string TipoConteudo);

public record PlanoDiarioOpcaoDto(decimal PrecoDiaria, decimal PrecoPorKm);

public record PlanoControladoOpcaoDto(int KmDisponiveisPorDia, decimal PrecoDiaria, decimal PrecoPorKmExtrapolado);

public record PlanoLivreOpcaoDto(decimal PrecoDiaria);

/// <summary>Uma taxa ou serviço que a locadora oferece (o cliente marca os que quer ao reservar).</summary>
public record TaxaOpcaoDto(Guid Id, string Nome, decimal Preco, TipoCobrancaTaxa TipoCobranca);

/// <summary>O que o cliente precisa para escolher como reservar: o carro, os 3 planos, os seguros e as taxas que a locadora oferece.</summary>
public record CarroDetalheDto(
    Guid Id,
    Guid LocadoraId,
    string Locadora,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel Combustivel,
    string Categoria,
    bool TemFoto,
    int QuantidadeFotos,
    PlanoDiarioOpcaoDto? Diario,
    PlanoControladoOpcaoDto? Controlado,
    PlanoLivreOpcaoDto? Livre,
    /// <summary>Vazio quando a locadora não oferece esse seguro.</summary>
    decimal? SeguroClienteDiaria,
    decimal? SeguroTerceirosDiaria,
    decimal ValorCaucao,
    IReadOnlyList<TaxaOpcaoDto>? Taxas = null
);

public record DetalharCarroQuery(Guid AutomovelId) : IRequest<Result<CarroDetalheDto>>;
