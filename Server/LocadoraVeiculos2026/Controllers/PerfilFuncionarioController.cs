using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloFuncionario;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>
/// Autoatendimento do perfil Funcionário: ele só enxerga e edita o próprio registro
/// (o id é sempre obtido do token, nunca da requisição).
/// </summary>
[Authorize(Roles = "Funcionario")]
[Route("api/funcionarios/meu-perfil")]
public sealed class PerfilFuncionarioController(IMediator mediator) : MainController
{
    [HttpGet]
    public async Task<ActionResult<SelecionarMeuPerfilResponse>> Selecionar(
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarMeuPerfilResult> result =
            await mediator.Send(new SelecionarMeuPerfilQuery(), cancellationToken);

        return ProcessarResultado(result, (valor) =>
        {
            SelecionarMeuPerfilResponse response = new(
                valor.Id,
                valor.NomeCompleto,
                valor.Cpf,
                valor.Email,
                valor.Salario,
                valor.AdmissaoEmUtc
            );

            return Ok(response);
        });
    }

    [HttpPut]
    public async Task<ActionResult<EditarMeuPerfilResponse>> Editar(
        EditarMeuPerfilRequest request,
        CancellationToken cancellationToken
    )
    {
        EditarMeuPerfilCommand command = new(request.NomeCompleto, request.Cpf);

        Result<EditarMeuPerfilResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            Ok(new EditarMeuPerfilResponse(valor.NomeCompleto, valor.Cpf)));
    }
}
