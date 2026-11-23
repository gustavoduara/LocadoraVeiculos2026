using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAutomovel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutomovel;

// Fotos extras (carrossel) de um carro: até AutomovelFoto.MaximoPorAutomovel, além da foto principal (capa).

public record FotoExtraDto(Guid Id, string NomeArquivo);

public record ListarFotosExtrasQuery(Guid AutomovelId) : IRequest<Result<IReadOnlyList<FotoExtraDto>>>;

public record AdicionarFotoExtraCommand(Guid AutomovelId, byte[] Conteudo, string NomeArquivo) : IRequest<Result<FotoExtraDto>>;

public record RemoverFotoExtraCommand(Guid AutomovelId, Guid FotoId) : IRequest<Result>;

public record ObterFotoExtraQuery(Guid AutomovelId, Guid FotoId) : IRequest<Result<Commands.FotoAutomovelResult>>;

public class FotosExtrasHandlers(
    AppDbContext dbContext,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    ILogger<FotosExtrasHandlers> logger
) :
    IRequestHandler<ListarFotosExtrasQuery, Result<IReadOnlyList<FotoExtraDto>>>,
    IRequestHandler<AdicionarFotoExtraCommand, Result<FotoExtraDto>>,
    IRequestHandler<RemoverFotoExtraCommand, Result>,
    IRequestHandler<ObterFotoExtraQuery, Result<Commands.FotoAutomovelResult>>
{
    public async Task<Result<IReadOnlyList<FotoExtraDto>>> Handle(ListarFotosExtrasQuery query, CancellationToken ct)
    {
        // O carro é buscado pelo repositório, que já filtra pela empresa: outra locadora recebe 404.
        if (await repositorioAutomovel.SelecionarPorIdAsync(query.AutomovelId) is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.AutomovelId));

        var fotos = await dbContext.AutomovelFotos
            .AsNoTracking()
            .Where(f => f.AutomovelId == query.AutomovelId)
            .OrderBy(f => f.Ordem).ThenBy(f => f.CriadaEmUtc)
            .Select(f => new FotoExtraDto(f.Id, f.NomeArquivo))
            .ToListAsync(ct);

        return Result.Ok<IReadOnlyList<FotoExtraDto>>(fotos);
    }

    public async Task<Result<FotoExtraDto>> Handle(AdicionarFotoExtraCommand command, CancellationToken ct)
    {
        if (await repositorioAutomovel.SelecionarPorIdAsync(command.AutomovelId) is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.AutomovelId));

        if (command.Conteudo.Length == 0)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A foto enviada está vazia."));

        if (command.Conteudo.Length > ImagemUtil.TamanhoMaximoEmBytes)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A foto deve ter no máximo 2 MB."));

        string? tipo = ImagemUtil.DetectarTipo(command.Conteudo);

        if (tipo is null)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A foto deve ser uma imagem JPEG, PNG ou WebP."));

        try
        {
            var existentes = await dbContext.AutomovelFotos
                .Where(f => f.AutomovelId == command.AutomovelId)
                .Select(f => f.Ordem)
                .ToListAsync(ct);

            if (existentes.Count >= AutomovelFoto.MaximoPorAutomovel)
                return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(
                    $"Cada carro pode ter até {AutomovelFoto.MaximoPorAutomovel} fotos extras, além da foto principal."));

            int ordem = existentes.Count == 0 ? 1 : existentes.Max() + 1;

            var foto = new AutomovelFoto(command.AutomovelId, ordem, command.Conteudo, tipo, ImagemUtil.NomeSeguro(command.NomeArquivo));

            dbContext.AutomovelFotos.Add(foto);
            await dbContext.SaveChangesAsync(ct);

            return Result.Ok(new FotoExtraDto(foto.Id, foto.NomeArquivo));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar uma foto extra do automóvel {Id}.", command.AutomovelId);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }

    public async Task<Result> Handle(RemoverFotoExtraCommand command, CancellationToken ct)
    {
        if (await repositorioAutomovel.SelecionarPorIdAsync(command.AutomovelId) is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.AutomovelId));

        var foto = await dbContext.AutomovelFotos
            .FirstOrDefaultAsync(f => f.Id == command.FotoId && f.AutomovelId == command.AutomovelId, ct);

        if (foto is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.FotoId));

        try
        {
            dbContext.AutomovelFotos.Remove(foto);
            await dbContext.SaveChangesAsync(ct);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao remover a foto extra {FotoId}.", command.FotoId);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }

    public async Task<Result<Commands.FotoAutomovelResult>> Handle(ObterFotoExtraQuery query, CancellationToken ct)
    {
        if (await repositorioAutomovel.SelecionarPorIdAsync(query.AutomovelId) is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.AutomovelId));

        var foto = await dbContext.AutomovelFotos
            .AsNoTracking()
            .Where(f => f.Id == query.FotoId && f.AutomovelId == query.AutomovelId)
            .Select(f => new { f.Conteudo, f.TipoConteudo })
            .FirstOrDefaultAsync(ct);

        if (foto is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.FotoId));

        return Result.Ok(new Commands.FotoAutomovelResult(foto.Conteudo, foto.TipoConteudo));
    }
}
