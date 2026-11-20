using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloParceiro;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/parceiros")]
public sealed class ParceiroController(IMediator mediator) : MainController
{
    /// <summary>Somente o administrador da empresa gerencia os parceiros.</summary>
    [Authorize(Roles = "Empresa")]
    [HttpPost]
    public async Task<ActionResult<CadastrarParceiroResponse>> Cadastrar(
        CadastrarParceiroRequest request, CancellationToken cancellationToken)
    {
        Result<CadastrarParceiroResult> result =
            await mediator.Send(new CadastrarParceiroCommand(request.Nome), cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarParceiroResponse(valor.Id)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EditarParceiroResponse>> Editar(
        Guid id, EditarParceiroRequest request, CancellationToken cancellationToken)
    {
        Result<EditarParceiroResult> result =
            await mediator.Send(new EditarParceiroCommand(id, request.Nome), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new EditarParceiroResponse(valor.Nome)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirParceiroResult> result =
            await mediator.Send(new ExcluirParceiroCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet]
    public async Task<ActionResult<SelecionarParceirosResponse>> SelecionarTodos(CancellationToken cancellationToken)
    {
        Result<SelecionarParceirosResult> result =
            await mediator.Send(new SelecionarParceirosQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarParceirosResponse(valor.Registros)));
    }

    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParceiroDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<ParceiroDto> result = await mediator.Send(new SelecionarParceiroPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
