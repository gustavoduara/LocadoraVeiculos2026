using FluentResults;
using LocadoraDeVeiculos.Aplicacao.ModuloAutomovel;
using LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Commands;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Models.ModuloAutomovel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

/// <summary>O cadastro de veículos é uma tarefa operacional: Empresa e Funcionário podem gerenciá-lo.</summary>
[Authorize(Roles = "Empresa,Funcionario")]
[Route("api/automoveis")]
public sealed class AutomovelController(IMediator mediator) : MainController
{
    [HttpPost]
    public async Task<ActionResult<CadastrarAutomovelResponse>> Cadastrar(
        CadastrarAutomovelRequest request,
        CancellationToken cancellationToken
    )
    {
        CadastrarAutomovelCommand command = new(
            request.Placa,
            request.Marca,
            request.Modelo,
            request.Cor,
            request.Ano,
            request.TipoCombustivel,
            request.CapacidadeCombustivelLitros,
            request.GrupoVeiculosId
        );

        Result<CadastrarAutomovelResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            CreatedAtAction(nameof(SelecionarPorId), new { id = valor.Id }, new CadastrarAutomovelResponse(valor.Id)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EditarAutomovelResponse>> Editar(
        Guid id,
        EditarAutomovelRequest request,
        CancellationToken cancellationToken
    )
    {
        EditarAutomovelCommand command = new(
            id,
            request.Placa,
            request.Marca,
            request.Modelo,
            request.Cor,
            request.Ano,
            request.TipoCombustivel,
            request.CapacidadeCombustivelLitros,
            request.GrupoVeiculosId
        );

        Result<EditarAutomovelResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) =>
            Ok(new EditarAutomovelResponse(
                valor.Placa,
                valor.Marca,
                valor.Modelo,
                valor.Cor,
                valor.Ano,
                valor.TipoCombustivel,
                valor.CapacidadeCombustivelLitros,
                valor.GrupoVeiculosId
            )));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        Result<ExcluirAutomovelResult> result =
            await mediator.Send(new ExcluirAutomovelCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }

    /// <summary>Lista a frota. O parâmetro opcional <c>grupoVeiculosId</c> filtra por grupo.</summary>
    [HttpGet]
    public async Task<ActionResult<SelecionarAutomoveisResponse>> SelecionarTodos(
        [FromQuery] Guid? grupoVeiculosId,
        CancellationToken cancellationToken
    )
    {
        Result<SelecionarAutomoveisResult> result =
            await mediator.Send(new SelecionarAutomoveisQuery(grupoVeiculosId), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new SelecionarAutomoveisResponse(valor.Registros)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AutomovelDto>> SelecionarPorId(Guid id, CancellationToken cancellationToken)
    {
        Result<AutomovelDto> result =
            await mediator.Send(new SelecionarAutomovelPorIdQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(valor));
    }

    /// <summary>Envia (ou substitui) a foto do veículo: multipart/form-data com o campo <c>arquivo</c>.</summary>
    [HttpPut("{id:guid}/foto")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<EnviarFotoAutomovelResponse>> EnviarFoto(
        Guid id,
        IFormFile arquivo,
        CancellationToken cancellationToken
    )
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new[] { "Requisição inválida", "Nenhuma foto foi enviada." });

        if (arquivo.Length > ImagemUtil.TamanhoMaximoEmBytes)
            return BadRequest(new[] { "Requisição inválida", "A foto deve ter no máximo 2 MB." });

        using MemoryStream memoria = new();
        await arquivo.CopyToAsync(memoria, cancellationToken);

        AtualizarFotoAutomovelCommand command = new(id, memoria.ToArray(), arquivo.FileName);

        Result<AtualizarFotoAutomovelResult> result = await mediator.Send(command, cancellationToken);

        return ProcessarResultado(result, (valor) => Ok(new EnviarFotoAutomovelResponse(valor.NomeArquivo)));
    }

    [HttpGet("{id:guid}/foto")]
    public async Task<ActionResult> ObterFoto(Guid id, CancellationToken cancellationToken)
    {
        Result<FotoAutomovelResult> result =
            await mediator.Send(new ObterFotoAutomovelQuery(id), cancellationToken);

        return ProcessarResultado(result, (valor) =>
        {
            // Tipo vem da assinatura real do arquivo; nosniff impede o navegador de reinterpretar.
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(valor.Conteudo, valor.TipoConteudo);
        });
    }

    [HttpDelete("{id:guid}/foto")]
    public async Task<ActionResult> RemoverFoto(Guid id, CancellationToken cancellationToken)
    {
        Result<RemoverFotoAutomovelResult> result =
            await mediator.Send(new RemoverFotoAutomovelCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }
}
