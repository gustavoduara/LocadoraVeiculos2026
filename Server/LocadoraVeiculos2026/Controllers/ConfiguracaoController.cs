using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloConfiguracao;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/configuracoes")]
public sealed class ConfiguracaoController(IMediator mediator) : MainController
{
    /// <summary>Leitura liberada aos dois perfis (o aluguel usa os preços no cálculo).</summary>
    [Authorize(Roles = "Empresa,Funcionario")]
    [HttpGet]
    public async Task<ActionResult<ConfiguracaoResponse>> Selecionar(
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarConfiguracaoResult> result =
            await mediator.Send(new SelecionarConfiguracaoQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) =>
            Ok(new ConfiguracaoResponse(
                valor.PrecoGasolina,
                valor.PrecoGas,
                valor.PrecoDiesel,
                valor.PrecoAlcool
            )));
    }

    /// <summary>Somente o administrador da empresa altera os preços.</summary>
    [Authorize(Roles = "Empresa")]
    [HttpPut]
    public async Task<ActionResult<ConfiguracaoResponse>> Editar(
        EditarConfiguracaoRequest request,
        CancellationToken cancellationToken
    )
    {
        EditarConfiguracaoCommand command = new(
            request.PrecoGasolina,
            request.PrecoGas,
            request.PrecoDiesel,
            request.PrecoAlcool
        );

        Result<EditarConfiguracaoResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            Ok(new ConfiguracaoResponse(
                valor.PrecoGasolina,
                valor.PrecoGas,
                valor.PrecoDiesel,
                valor.PrecoAlcool
            )));
    }
}
