using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloPlanoCobranca;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/planos-cobranca")]
public sealed class PlanoCobrancaController(IMediator mediator) : MainController
{
    /// <summary>Somente o administrador da empresa define os preços dos planos.</summary>
    [Authorize(Roles = "Empresa")]
    [HttpPost]
    public async Task<ActionResult<CadastrarPlanoCobrancaResponse>> Cadastrar(
        CadastrarPlanoCobrancaRequest request,
        CancellationToken cancellationToken
    )
    {
        CadastrarPlanoCobrancaCommand command = new(
            request.AutomovelId,
            request.Diario,
            request.Controlado,
            request.Livre
        );

        Result<CadastrarPlanoCobrancaResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarPlanoCobrancaResponse(valor.Id)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EditarPlanoCobrancaResponse>> Editar(
        Guid id,
        EditarPlanoCobrancaRequest request,
        CancellationToken cancellationToken
    )
    {
        EditarPlanoCobrancaCommand command = new(id, request.Diario, request.Controlado, request.Livre);

        Result<EditarPlanoCobrancaResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            Ok(new EditarPlanoCobrancaResponse(valor.Diario, valor.Controlado, valor.Livre)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        Result<ExcluirPlanoCobrancaResult> result =
            await mediator.Send(new ExcluirPlanoCobrancaCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Leitura liberada aos dois perfis (o aluguel usa os planos no cálculo).</summary>
    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet]
    public async Task<ActionResult<SelecionarPlanosCobrancaResponse>> SelecionarTodos(
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarPlanosCobrancaResult> result =
            await mediator.Send(new SelecionarPlanosCobrancaQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarPlanosCobrancaResponse(valor.Registros)));
    }

    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlanoCobrancaDto>> SelecionarPorId(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        Result<PlanoCobrancaDto> result =
            await mediator.Send(new SelecionarPlanoCobrancaPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
