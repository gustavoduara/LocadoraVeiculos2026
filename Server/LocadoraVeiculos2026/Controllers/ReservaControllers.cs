using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloPerfilCliente.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloReserva.Commands;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloReserva;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>Os dados que o cliente informa uma vez (documentos, contato, endereço) para poder reservar.</summary>
[Authorize(Roles = "Cliente")]
[Route("api/meus-dados")]
public sealed class MeusDadosController(IMediator mediator) : MainController
{
    [HttpGet]
    public async Task<ActionResult<PerfilClienteDto>> Selecionar(CancellationToken cancellationToken)
    {
        Result<PerfilClienteDto> result = await mediator.Send(new SelecionarMeusDadosQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpPut]
    public async Task<ActionResult<PerfilClienteDto>> Salvar(SalvarMeusDadosRequest request, CancellationToken cancellationToken)
    {
        Result<PerfilClienteDto> result = await mediator.Send(
            new SalvarMeusDadosCommand(request.ParaDados()), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}

/// <summary>Pedidos de reserva vistos pelo cliente: calcular, pedir, acompanhar e cancelar.</summary>
[Authorize(Roles = "Cliente")]
[Route("api/minhas-reservas")]
public sealed class MinhasReservasController(IMediator mediator) : MainController
{
    /// <summary>Estima o valor do pedido sem enviar nada.</summary>
    [HttpPost("calcular")]
    public async Task<ActionResult<EstimativaReservaDto>> Calcular(DadosReservaRequest request, CancellationToken cancellationToken)
    {
        Result<EstimativaReservaDto> result = await mediator.Send(new CalcularReservaQuery(request.ParaDados()), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpPost]
    public async Task<ActionResult<CriarReservaResult>> Criar(DadosReservaRequest request, CancellationToken cancellationToken)
    {
        Result<CriarReservaResult> result = await mediator.Send(new CriarReservaCommand(request.ParaDados()), cancellationToken);

        return ProcessarResultado(result, (valor) => Created($"api/minhas-reservas", valor));
    }

    [HttpGet]
    public async Task<ActionResult<SelecionarReservasResult>> Listar(CancellationToken cancellationToken)
    {
        Result<SelecionarReservasResult> result = await mediator.Send(new SelecionarMinhasReservasQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<ReservaDto>> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        Result<ReservaDto> result = await mediator.Send(new CancelarMinhaReservaCommand(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}

/// <summary>Pedidos de reserva recebidos pela locadora: ver, aprovar (vira um aluguel) ou recusar.</summary>
[Authorize(Roles = "Empresa,Funcionario")]
[Route("api/reservas")]
public sealed class ReservaController(IMediator mediator) : MainController
{
    [HttpGet]
    public async Task<ActionResult<SelecionarReservasResult>> Listar([FromQuery] StatusReserva? status, CancellationToken cancellationToken)
    {
        Result<SelecionarReservasResult> result = await mediator.Send(new SelecionarReservasRecebidasQuery(status), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpGet("pendentes/total")]
    public async Task<ActionResult<TotalDePendentesResponse>> TotalDePendentes(CancellationToken cancellationToken)
    {
        Result<int> result = await mediator.Send(new ContarReservasPendentesQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new TotalDePendentesResponse(valor)));
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<ActionResult<ReservaDto>> Aprovar(Guid id, AprovarReservaRequest request, CancellationToken cancellationToken)
    {
        Result<ReservaDto> result = await mediator.Send(new AprovarReservaCommand(id, request.KmInicial), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [HttpPost("{id:guid}/recusar")]
    public async Task<ActionResult<ReservaDto>> Recusar(Guid id, RecusarReservaRequest request, CancellationToken cancellationToken)
    {
        Result<ReservaDto> result = await mediator.Send(new RecusarReservaCommand(id, request.Motivo), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }
}
