using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloCupom;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/cupons")]
public sealed class CupomController(IMediator mediator) : MainController
{
    /// <summary>Somente o administrador da empresa gerencia os cupons.</summary>
    [Authorize(Roles = "Empresa")]
    [HttpPost]
    public async Task<ActionResult<CadastrarCupomResponse>> Cadastrar(
        CadastrarCupomRequest request, CancellationToken cancellationToken)
    {
        CadastrarCupomCommand command = new(request.Nome, request.Valor, request.DataValidade, request.ParceiroId);

        Result<CadastrarCupomResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarCupomResponse(valor.Id)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CupomDto>> Editar(
        Guid id, EditarCupomRequest request, CancellationToken cancellationToken)
    {
        EditarCupomCommand command = new(id, request.Nome, request.Valor, request.DataValidade, request.ParceiroId);

        Result<EditarCupomResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor.Cupom));
    }

    [Authorize(Roles = "Empresa")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirCupomResult> result = await mediator.Send(new ExcluirCupomCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Leitura liberada aos dois perfis (o aluguel escolhe o cupom na lista).</summary>
    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet]
    public async Task<ActionResult<SelecionarCuponsResponse>> SelecionarTodos(CancellationToken cancellationToken)
    {
        Result<SelecionarCuponsResult> result = await mediator.Send(new SelecionarCuponsQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarCuponsResponse(valor.Registros)));
    }

    /// <summary>Ranking de uso dos cupons e dos parceiros: serve para investir em quem gera mais retorno.</summary>
    [Authorize(Roles = "Empresa")]
    [HttpGet("mais-utilizados")]
    public async Task<ActionResult<CuponsMaisUtilizadosResult>> MaisUtilizados(CancellationToken cancellationToken)
    {
        Result<CuponsMaisUtilizadosResult> result =
            await mediator.Send(new CuponsMaisUtilizadosQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CupomDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<CupomDto> result = await mediator.Send(new SelecionarCupomPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
