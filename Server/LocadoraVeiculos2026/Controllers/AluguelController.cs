using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloAluguel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>O registro de locações é uma tarefa operacional: Empresa e Funcionário podem gerenciá-lo.</summary>
[Authorize(Roles = "Empresa,Funcionario")]
[Route("api/alugueis")]
public sealed class AluguelController(IMediator mediator) : MainController
{
    [HttpPost]
    public async Task<ActionResult<CadastrarAluguelResponse>> Cadastrar(
        DadosAluguelRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<CadastrarAluguelResult> result =
            await mediator.Send(new CadastrarAluguelCommand(request.ParaDados()), cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarAluguelResponse(valor.Id)));
    }

    /// <summary>Simula o valor previsto sem gravar nada (usado pela tela para mostrar o Valor Total).</summary>
    [HttpPost("calcular")]
    public async Task<ActionResult<SimulacaoAluguelDto>> Calcular(
        DadosAluguelRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<SimulacaoAluguelDto> result =
            await mediator.Send(new CalcularAluguelQuery(request.ParaDados()), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AluguelDto>> Editar(
        Guid id,
        DadosAluguelRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<EditarAluguelResult> result =
            await mediator.Send(new EditarAluguelCommand(id, request.ParaDados()), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor.Aluguel));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirAluguelResult> result =
            await mediator.Send(new ExcluirAluguelCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Lista os aluguéis. O parâmetro opcional <c>status</c> filtra por EmAberto ou Concluido.</summary>
    [HttpGet]
    public async Task<ActionResult<SelecionarAlugueisResponse>> SelecionarTodos(
        [FromQuery] StatusAluguel? status,
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarAlugueisResult> result =
            await mediator.Send(new SelecionarAlugueisQuery(status), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarAlugueisResponse(valor.Registros)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AluguelDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<AluguelDto> result =
            await mediator.Send(new SelecionarAluguelPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    /// <summary>Registra a devolução: calcula o preço final (km, combustível e multa por atraso) e conclui o aluguel.</summary>
    [HttpPost("{id:guid}/devolucao")]
    public async Task<ActionResult<AluguelDto>> Devolver(
        Guid id,
        DevolucaoRequest request,
        CancellationToken cancellationToken
    )
    {
        DevolverAluguelCommand command = new(id, request.DataDevolucao, request.KmFinal, request.TanqueCheio);

        Result<DevolverAluguelResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor.Aluguel));
    }

    /// <summary>Simula o preço final da devolução sem concluir o aluguel.</summary>
    [HttpPost("{id:guid}/devolucao/calcular")]
    public async Task<ActionResult<DetalhamentoFinal>> CalcularDevolucao(
        Guid id,
        DevolucaoRequest request,
        CancellationToken cancellationToken
    )
    {
        CalcularDevolucaoQuery query = new(id, request.DataDevolucao, request.KmFinal, request.TanqueCheio);

        Result<DetalhamentoFinal> result = await mediator.Send(query, cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
