using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloCondutor;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>O cadastro de condutores é uma tarefa operacional: Empresa e Funcionário podem gerenciá-lo.</summary>
[Authorize(Roles = "Empresa,Funcionario")]
[Route("api/condutores")]
public sealed class CondutorController(IMediator mediator) : MainController
{
    [HttpPost]
    public async Task<ActionResult<CadastrarCondutorResponse>> Cadastrar(
        CadastrarCondutorRequest request,
        CancellationToken cancellationToken
    )
    {
        CadastrarCondutorCommand command = new(
            request.ClienteId,
            request.Nome,
            request.Email,
            request.Telefone,
            request.Cpf,
            request.Cnh,
            request.ValidadeCnh
        );

        Result<CadastrarCondutorResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarCondutorResponse(valor.Id)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CondutorDto>> Editar(
        Guid id,
        EditarCondutorRequest request,
        CancellationToken cancellationToken
    )
    {
        EditarCondutorCommand command = new(
            id,
            request.Nome,
            request.Email,
            request.Telefone,
            request.Cpf,
            request.Cnh,
            request.ValidadeCnh
        );

        Result<EditarCondutorResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor.Condutor));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirCondutorResult> result =
            await mediator.Send(new ExcluirCondutorCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Lista os condutores. O parâmetro opcional <c>clienteId</c> filtra pelos de um cliente.</summary>
    [HttpGet]
    public async Task<ActionResult<SelecionarCondutoresResponse>> SelecionarTodos(
        [FromQuery] Guid? clienteId,
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarCondutoresResult> result =
            await mediator.Send(new SelecionarCondutoresQuery(clienteId), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarCondutoresResponse(valor.Registros)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CondutorDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<CondutorDto> result =
            await mediator.Send(new SelecionarCondutorPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
