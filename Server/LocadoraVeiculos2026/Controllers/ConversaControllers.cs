using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloConversa;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Config.Seguranca;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocadoraDeVeiculos.WebApi.Controllers;

public record IniciarConversaRequest(Guid AutomovelId, string? Texto);

/// <summary><c>AutomovelId</c> é opcional: etiqueta a mensagem com um carro da mesma locadora.</summary>
public record EnviarMensagemRequest(string? Texto, Guid? AutomovelId = null);

public record ConversasResponse(IReadOnlyList<ConversaResumoDto> Registros);

public record TotalNaoLidasResponse(int Total);

/// <summary>Lado do cliente: ele escreve para a locadora de um carro e acompanha as respostas.</summary>
[Authorize(Roles = "Cliente")]
[Route("api/minhas-conversas")]
public sealed class MinhasConversasController(IMediator mediator) : MainController
{
    [HttpGet]
    public async Task<ActionResult<ConversasResponse>> Listar(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ConversaResumoDto>> result = await mediator.Send(new ListarConversasDoClienteQuery(), cancellationToken);

        return ProcessarResultado(result, (lista) => Ok(new ConversasResponse(lista)));
    }

    [HttpGet("nao-lidas/total")]
    public async Task<ActionResult<TotalNaoLidasResponse>> TotalNaoLidas(CancellationToken cancellationToken)
    {
        Result<int> result = await mediator.Send(new TotalNaoLidasDoClienteQuery(), cancellationToken);

        return ProcessarResultado(result, (total) => Ok(new TotalNaoLidasResponse(total)));
    }

    /// <summary>Abre a conversa e marca como lidas as mensagens da locadora.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversaDetalheDto>> Abrir(Guid id, CancellationToken cancellationToken)
    {
        Result<ConversaDetalheDto> result = await mediator.Send(new AbrirConversaDoClienteQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    /// <summary>Começa (ou retoma) a conversa com a locadora do carro e envia a primeira mensagem.</summary>
    [EnableRateLimiting(SegurancaConfig.PoliticaDeMensagens)]
    [HttpPost]
    public async Task<ActionResult> Iniciar(IniciarConversaRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> result = await mediator.Send(new IniciarConversaCommand(request.AutomovelId, request.Texto), cancellationToken);

        return ProcessarResultado(result, (id) => CreatedAtAction(nameof(Abrir), new { id }, new { id }));
    }

    [EnableRateLimiting(SegurancaConfig.PoliticaDeMensagens)]
    [HttpPost("{id:guid}/mensagens")]
    public async Task<ActionResult<MensagemDto>> Enviar(Guid id, EnviarMensagemRequest request, CancellationToken cancellationToken)
    {
        Result<MensagemDto> result = await mediator.Send(
            new EnviarMensagemDoClienteCommand(id, request.Texto, request.AutomovelId), cancellationToken);

        return ProcessarResultado(result, (valor) => StatusCode(201, valor));
    }
}

/// <summary>Lado da locadora: a caixa de entrada compartilhada entre o administrador e os funcionários.</summary>
[Authorize(Roles = "Empresa,Funcionario")]
[Route("api/conversas")]
public sealed class ConversasController(IMediator mediator) : MainController
{
    [HttpGet]
    public async Task<ActionResult<ConversasResponse>> Listar(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ConversaResumoDto>> result = await mediator.Send(new ListarConversasDaLocadoraQuery(), cancellationToken);

        return ProcessarResultado(result, (lista) => Ok(new ConversasResponse(lista)));
    }

    [HttpGet("nao-lidas/total")]
    public async Task<ActionResult<TotalNaoLidasResponse>> TotalNaoLidas(CancellationToken cancellationToken)
    {
        Result<int> result = await mediator.Send(new TotalNaoLidasDaLocadoraQuery(), cancellationToken);

        return ProcessarResultado(result, (total) => Ok(new TotalNaoLidasResponse(total)));
    }

    /// <summary>Abre a conversa e marca como lidas as mensagens do cliente.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversaDetalheDto>> Abrir(Guid id, CancellationToken cancellationToken)
    {
        Result<ConversaDetalheDto> result = await mediator.Send(new AbrirConversaDaLocadoraQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    [EnableRateLimiting(SegurancaConfig.PoliticaDeMensagens)]
    [HttpPost("{id:guid}/mensagens")]
    public async Task<ActionResult<MensagemDto>> Enviar(Guid id, EnviarMensagemRequest request, CancellationToken cancellationToken)
    {
        Result<MensagemDto> result = await mediator.Send(new EnviarMensagemDaLocadoraCommand(id, request.Texto), cancellationToken);

        return ProcessarResultado(result, (valor) => StatusCode(201, valor));
    }
}
