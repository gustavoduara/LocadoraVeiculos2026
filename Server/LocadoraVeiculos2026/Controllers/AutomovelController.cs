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
public record FotosExtrasResponse(IReadOnlyList<FotoExtraDto> Registros);

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
            request.GrupoVeiculosId,
            request.SeguroDiaria
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
            request.GrupoVeiculosId,
            request.SeguroDiaria
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
                valor.GrupoVeiculosId,
                valor.SeguroDiaria
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

    // ---------------- fotos extras (carrossel) ----------------

    /// <summary>As fotos extras do carro, na ordem do carrossel (a foto principal é a do endpoint <c>foto</c>).</summary>
    [HttpGet("{id:guid}/fotos")]
    public async Task<ActionResult<FotosExtrasResponse>> ListarFotosExtras(Guid id, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<FotoExtraDto>> result = await mediator.Send(new ListarFotosExtrasQuery(id), cancellationToken);

        return ProcessarResultado(result, (lista) => Ok(new FotosExtrasResponse(lista)));
    }

    /// <summary>Acrescenta uma foto extra (até 5): multipart/form-data com o campo <c>arquivo</c>.</summary>
    [HttpPost("{id:guid}/fotos")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<FotoExtraDto>> AdicionarFotoExtra(
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

        Result<FotoExtraDto> result = await mediator.Send(
            new AdicionarFotoExtraCommand(id, memoria.ToArray(), arquivo.FileName), cancellationToken);

        return ProcessarResultado(result, (valor) => StatusCode(201, valor));
    }

    [HttpGet("{id:guid}/fotos/{fotoId:guid}")]
    public async Task<ActionResult> ObterFotoExtra(Guid id, Guid fotoId, CancellationToken cancellationToken)
    {
        Result<FotoAutomovelResult> result = await mediator.Send(new ObterFotoExtraQuery(id, fotoId), cancellationToken);

        return ProcessarResultado(result, (valor) =>
        {
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(valor.Conteudo, valor.TipoConteudo);
        });
    }

    [HttpDelete("{id:guid}/fotos/{fotoId:guid}")]
    public async Task<ActionResult> RemoverFotoExtra(Guid id, Guid fotoId, CancellationToken cancellationToken)
    {
        Result result = await mediator.Send(new RemoverFotoExtraCommand(id, fotoId), cancellationToken);

        return ProcessarResultado(result, () => NoContent());
    }

    [HttpDelete("{id:guid}/foto")]
    public async Task<ActionResult> RemoverFoto(Guid id, CancellationToken cancellationToken)
    {
        Result<RemoverFotoAutomovelResult> result =
            await mediator.Send(new RemoverFotoAutomovelCommand(id), cancellationToken);

        return ProcessarResultado(result, (_) => NoContent());
    }
}
