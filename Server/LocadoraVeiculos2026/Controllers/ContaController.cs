using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloConta;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Config.Seguranca;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocadoraDeVeiculos.WebApi.Controllers;

public record ExcluirMinhaContaRequest(string? Senha, string? Confirmacao);

public record AgendarExclusaoRequest(string? Senha, string? NomeDaEmpresa);

/// <summary>Exclusão da própria conta (LGPD): cliente na hora, locadora agendada com prazo.</summary>
[Route("api/conta")]
[Authorize]
public sealed class ContaController(IMediator mediator) : MainController
{
    /// <summary>Se a conta tem uma exclusão agendada (a locadora vê o aviso com a data).</summary>
    [HttpGet("situacao")]
    public async Task<ActionResult<SituacaoDaContaDto>> Situacao(CancellationToken cancellationToken)
    {
        Result<SituacaoDaContaDto> result = await mediator.Send(new ObterSituacaoDaContaQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    /// <summary>O cliente exclui a conta agora. Pede a senha e a palavra EXCLUIR.</summary>
    [Authorize(Roles = "Cliente")]
    [EnableRateLimiting(SegurancaConfig.PoliticaDeLogin)]
    [HttpPost("excluir")]
    public async Task<ActionResult> Excluir(ExcluirMinhaContaRequest request, CancellationToken cancellationToken)
    {
        Result result = await mediator.Send(new ExcluirMinhaContaCommand(request.Senha, request.Confirmacao), cancellationToken);

        return ProcessarResultado(result, () => NoContent());
    }

    /// <summary>O administrador da locadora agenda a exclusão da empresa (vale em 7 dias e dá para cancelar).</summary>
    [Authorize(Roles = "Empresa")]
    [EnableRateLimiting(SegurancaConfig.PoliticaDeLogin)]
    [HttpPost("agendar-exclusao")]
    public async Task<ActionResult<SituacaoDaContaDto>> AgendarExclusao(AgendarExclusaoRequest request, CancellationToken cancellationToken)
    {
        Result<DateTime> result = await mediator.Send(new AgendarExclusaoCommand(request.Senha, request.NomeDaEmpresa), cancellationToken);

        return ProcessarResultado(result, (data) => Ok(new SituacaoDaContaDto(data)));
    }

    [Authorize(Roles = "Empresa")]
    [HttpPost("cancelar-exclusao")]
    public async Task<ActionResult> CancelarExclusao(CancellationToken cancellationToken)
    {
        Result result = await mediator.Send(new CancelarExclusaoCommand(), cancellationToken);

        return ProcessarResultado(result, () => NoContent());
    }
}
