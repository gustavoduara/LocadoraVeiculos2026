using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloCliente;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>O cadastro de clientes é uma tarefa operacional: Empresa e Funcionário podem gerenciá-lo.</summary>
[Authorize(Roles = "Empresa,Funcionario")]
[Route("api/clientes")]
public sealed class ClienteController(IMediator mediator) : MainController
{
    [HttpPost]
    public async Task<ActionResult<CadastrarClienteResponse>> Cadastrar(
        CadastrarClienteRequest request,
        CancellationToken cancellationToken
    )
    {
        DadosClienteDto dados = new(
            request.Nome,
            request.Email,
            request.Telefone,
            request.Cpf,
            request.Rg,
            request.Cnh,
            request.Cnpj,
            request.Estado,
            request.Cidade,
            request.Bairro,
            request.Rua,
            request.Numero,
            request.PessoaJuridicaId
        );

        Result<CadastrarClienteResult> result =
            await mediator.Send(new CadastrarClienteCommand(request.Tipo, dados), cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarClienteResponse(valor.Id)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> Editar(
        Guid id,
        EditarClienteRequest request,
        CancellationToken cancellationToken
    )
    {
        DadosClienteDto dados = new(
            request.Nome,
            request.Email,
            request.Telefone,
            request.Cpf,
            request.Rg,
            request.Cnh,
            request.Cnpj,
            request.Estado,
            request.Cidade,
            request.Bairro,
            request.Rua,
            request.Numero,
            request.PessoaJuridicaId
        );

        Result<EditarClienteResult> result =
            await mediator.Send(new EditarClienteCommand(id, dados), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor.Cliente));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirClienteResult> result =
            await mediator.Send(new ExcluirClienteCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Lista os clientes. O parâmetro opcional <c>tipo</c> filtra por PessoaFisica ou PessoaJuridica.</summary>
    [HttpGet]
    public async Task<ActionResult<SelecionarClientesResponse>> SelecionarTodos(
        [FromQuery] TipoCliente? tipo,
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarClientesResult> result =
            await mediator.Send(new SelecionarClientesQuery(tipo), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarClientesResponse(valor.Registros)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<ClienteDto> result =
            await mediator.Send(new SelecionarClientePorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
