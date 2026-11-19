using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloTaxaServico;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/taxas-servicos")]
public sealed class TaxaServicoController(IMediator mediator) : MainController
{
    /// <summary>Somente o administrador da empresa define o catálogo de taxas e preços.</summary>
    [Authorize(Roles = "Empresa")]
    [HttpPost]
    public async Task<ActionResult<CadastrarTaxaServicoResponse>> Cadastrar(
        CadastrarTaxaServicoRequest request,
        CancellationToken cancellationToken
    )
    {
        CadastrarTaxaServicoCommand command = new(request.Nome, request.Preco, request.TipoCobranca);

        Result<CadastrarTaxaServicoResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarTaxaServicoResponse(valor.Id)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EditarTaxaServicoResponse>> Editar(
        Guid id,
        EditarTaxaServicoRequest request,
        CancellationToken cancellationToken
    )
    {
        EditarTaxaServicoCommand command = new(id, request.Nome, request.Preco, request.TipoCobranca);

        Result<EditarTaxaServicoResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            Ok(new EditarTaxaServicoResponse(valor.Nome, valor.Preco, valor.TipoCobranca)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirTaxaServicoResult> result =
            await mediator.Send(new ExcluirTaxaServicoCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Leitura liberada aos dois perfis (o aluguel usa o catálogo no cálculo).</summary>
    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet]
    public async Task<ActionResult<SelecionarTaxasServicosResponse>> SelecionarTodos(
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarTaxasServicosResult> result =
            await mediator.Send(new SelecionarTaxasServicosQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarTaxasServicosResponse(valor.Registros)));
    }

    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxaServicoDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<TaxaServicoDto> result =
            await mediator.Send(new SelecionarTaxaServicoPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
