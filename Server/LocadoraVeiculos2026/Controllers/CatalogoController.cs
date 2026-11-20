using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloCatalogo.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>
/// Catálogo de carros de todas as locadoras, para o cliente procurar o que quer.
/// Só a conta de cliente usa: as locadoras não enxergam a frota umas das outras.
/// </summary>
[Authorize(Roles = "Cliente")]
[Route("api/catalogo")]
public sealed class CatalogoController(IMediator mediator) : MainController
{
    /// <summary>Datas no formato ISO (yyyy-MM-dd). Todos os filtros são opcionais.</summary>
    [HttpGet]
    public async Task<ActionResult<CatalogoResult>> Pesquisar(
        [FromQuery] DateOnly? retirada,
        [FromQuery] DateOnly? devolucao,
        [FromQuery] string? texto,
        [FromQuery] string? categoria,
        [FromQuery] TipoCombustivel? combustivel,
        [FromQuery] int? anoMinimo,
        [FromQuery] decimal? diariaMaxima,
        [FromQuery] Guid? locadoraId,
        [FromQuery] bool apenasDisponiveis = true,
        [FromQuery] OrdenacaoCatalogo ordenacao = OrdenacaoCatalogo.MenorPreco,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 12,
        CancellationToken cancellationToken = default)
    {
        var query = new PesquisarCatalogoQuery(
            retirada, devolucao, texto, categoria, combustivel, anoMinimo, diariaMaxima,
            locadoraId, apenasDisponiveis, ordenacao, pagina, tamanhoPagina);

        Result<CatalogoResult> result = await mediator.Send(query, cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    /// <summary>Opções para montar os filtros: categorias, marcas e locadoras que existem hoje.</summary>
    [HttpGet("filtros")]
    public async Task<ActionResult<FiltrosDoCatalogoResult>> Filtros(CancellationToken cancellationToken)
    {
        Result<FiltrosDoCatalogoResult> result = await mediator.Send(new FiltrosDoCatalogoQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    /// <summary>Detalhe de um carro: os 3 planos e os seguros que a locadora oferece (para escolher como reservar).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CarroDetalheDto>> Detalhar(Guid id, CancellationToken cancellationToken)
    {
        Result<CarroDetalheDto> result = await mediator.Send(new DetalharCarroQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpGet("{id:guid}/foto")]
    public async Task<ActionResult> Foto(Guid id, CancellationToken cancellationToken)
    {
        Result<FotoDoCatalogoResult> result = await mediator.Send(new FotoDoCatalogoQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => File(valor.Conteudo, valor.TipoConteudo));
    }
}
