using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Dominio.ModuloGrupoVeiculos;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAutomovel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloGrupoVeiculos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Handlers;

internal static class AutomovelMapeamento
{
    public static string NormalizarPlaca(string placa) =>
        placa.Replace("-", string.Empty).Trim().ToUpperInvariant();

    public static AutomovelDto ParaDto(this Automovel a) => new(
        a.Id,
        a.Placa,
        a.Marca,
        a.Modelo,
        a.Cor,
        a.Ano,
        a.TipoCombustivel,
        a.CapacidadeCombustivelLitros,
        a.GrupoVeiculosId,
        a.GrupoVeiculos?.Nome ?? string.Empty,
        a.TemFoto,
        a.FotoNomeArquivo
    );

    public static AutomovelDto ParaDto(this AutomovelResumo r) => new(
        r.Id,
        r.Placa,
        r.Marca,
        r.Modelo,
        r.Cor,
        r.Ano,
        r.TipoCombustivel,
        r.CapacidadeCombustivelLitros,
        r.GrupoVeiculosId,
        r.NomeGrupo,
        r.TemFoto,
        r.FotoNomeArquivo
    );
}

public class CadastrarAutomovelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    RepositorioGrupoVeiculosEmOrm repositorioGrupo,
    ITenantProvider tenantProvider,
    IValidator<CadastrarAutomovelCommand> validator,
    ILogger<CadastrarAutomovelCommandHandler> logger
) : IRequestHandler<CadastrarAutomovelCommand, Result<CadastrarAutomovelResult>>
{
    public async Task<Result<CadastrarAutomovelResult>> Handle(
        CadastrarAutomovelCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        GrupoVeiculos? grupo = await repositorioGrupo.SelecionarPorIdAsync(command.GrupoVeiculosId);

        if (grupo is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.GrupoVeiculosId));

        string placa = AutomovelMapeamento.NormalizarPlaca(command.Placa);

        if (await repositorioAutomovel.ExistePlacaAsync(placa))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um automóvel cadastrado com esta placa."));

        try
        {
            Automovel automovel = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                placa,
                command.Marca.Trim(),
                command.Modelo.Trim(),
                command.Cor.Trim(),
                command.Ano,
                command.TipoCombustivel,
                command.CapacidadeCombustivelLitros,
                grupo.Id
            );

            await repositorioAutomovel.CadastrarAsync(automovel);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarAutomovelResult(automovel.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarAutomovelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    RepositorioGrupoVeiculosEmOrm repositorioGrupo,
    ITenantProvider tenantProvider,
    RepositorioAluguelEmOrm repositorioAluguel,
    IValidator<EditarAutomovelCommand> validator,
    ILogger<EditarAutomovelCommandHandler> logger
) : IRequestHandler<EditarAutomovelCommand, Result<EditarAutomovelResult>>
{
    public async Task<Result<EditarAutomovelResult>> Handle(
        EditarAutomovelCommand command, CancellationToken cancellationToken)
    {
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(command.Id);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        GrupoVeiculos? grupo = await repositorioGrupo.SelecionarPorIdAsync(command.GrupoVeiculosId);

        if (grupo is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.GrupoVeiculosId));

        string placa = AutomovelMapeamento.NormalizarPlaca(command.Placa);

        if (await repositorioAutomovel.ExistePlacaAsync(placa, ignorarId: automovel.Id))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um automóvel cadastrado com esta placa."));

        if (await repositorioAluguel.ExisteEmAbertoParaAutomovelAsync(automovel.Id))
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro(
                "Não é possível editar um automóvel em um aluguel ainda não concluído."));


        try
        {
            Automovel automovelEditado = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                placa,
                command.Marca.Trim(),
                command.Modelo.Trim(),
                command.Cor.Trim(),
                command.Ano,
                command.TipoCombustivel,
                command.CapacidadeCombustivelLitros,
                grupo.Id
            );

            automovel.AtualizarRegistro(automovelEditado);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarAutomovelResult(
                placa,
                automovelEditado.Marca,
                automovelEditado.Modelo,
                automovelEditado.Cor,
                automovelEditado.Ano,
                automovelEditado.TipoCombustivel,
                automovelEditado.CapacidadeCombustivelLitros,
                grupo.Id
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirAutomovelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    RepositorioAluguelEmOrm repositorioAluguel,
    ILogger<ExcluirAutomovelCommandHandler> logger
) : IRequestHandler<ExcluirAutomovelCommand, Result<ExcluirAutomovelResult>>
{
    public async Task<Result<ExcluirAutomovelResult>> Handle(
        ExcluirAutomovelCommand command, CancellationToken cancellationToken)
    {
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(command.Id);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (await repositorioAluguel.ExisteEmAbertoParaAutomovelAsync(automovel.Id))
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir um automóvel em um aluguel ainda não concluído."));


        try
        {
            await repositorioAutomovel.ExcluirAsync(automovel.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirAutomovelResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarAutomovelPorIdQueryHandler(
    RepositorioAutomovelEmOrm repositorioAutomovel
) : IRequestHandler<SelecionarAutomovelPorIdQuery, Result<AutomovelDto>>
{
    public async Task<Result<AutomovelDto>> Handle(
        SelecionarAutomovelPorIdQuery query, CancellationToken cancellationToken)
    {
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(query.Id);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(automovel.ParaDto());
    }
}

public class SelecionarAutomoveisQueryHandler(
    RepositorioAutomovelEmOrm repositorioAutomovel
) : IRequestHandler<SelecionarAutomoveisQuery, Result<SelecionarAutomoveisResult>>
{
    public async Task<Result<SelecionarAutomoveisResult>> Handle(
        SelecionarAutomoveisQuery query, CancellationToken cancellationToken)
    {
        List<AutomovelResumo> resumos = await repositorioAutomovel.SelecionarResumosAsync(query.GrupoVeiculosId);

        return Result.Ok(new SelecionarAutomoveisResult(resumos.Select(r => r.ParaDto()).ToList()));
    }
}

public class AtualizarFotoAutomovelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    ILogger<AtualizarFotoAutomovelCommandHandler> logger
) : IRequestHandler<AtualizarFotoAutomovelCommand, Result<AtualizarFotoAutomovelResult>>
{
    public async Task<Result<AtualizarFotoAutomovelResult>> Handle(
        AtualizarFotoAutomovelCommand command, CancellationToken cancellationToken)
    {
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(command.Id);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (command.Conteudo.Length == 0)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A foto enviada está vazia."));

        if (command.Conteudo.Length > ImagemUtil.TamanhoMaximoEmBytes)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A foto deve ter no máximo 2 MB."));

        string? tipo = ImagemUtil.DetectarTipo(command.Conteudo);

        if (tipo is null)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                "A foto deve ser uma imagem JPEG, PNG ou WebP."));

        try
        {
            string nome = ImagemUtil.NomeSeguro(command.NomeArquivo);

            automovel.AtualizarFoto(command.Conteudo, tipo, nome);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new AtualizarFotoAutomovelResult(nome));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro ao salvar a foto do automóvel {Id}.", command.Id);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class RemoverFotoAutomovelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    ILogger<RemoverFotoAutomovelCommandHandler> logger
) : IRequestHandler<RemoverFotoAutomovelCommand, Result<RemoverFotoAutomovelResult>>
{
    public async Task<Result<RemoverFotoAutomovelResult>> Handle(
        RemoverFotoAutomovelCommand command, CancellationToken cancellationToken)
    {
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(command.Id);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        try
        {
            automovel.RemoverFoto();

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new RemoverFotoAutomovelResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro ao remover a foto do automóvel {Id}.", command.Id);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ObterFotoAutomovelQueryHandler(
    RepositorioAutomovelEmOrm repositorioAutomovel
) : IRequestHandler<ObterFotoAutomovelQuery, Result<FotoAutomovelResult>>
{
    public async Task<Result<FotoAutomovelResult>> Handle(
        ObterFotoAutomovelQuery query, CancellationToken cancellationToken)
    {
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(query.Id);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        if (!automovel.TemFoto || automovel.FotoTipoConteudo is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Foto do automóvel."));

        return Result.Ok(new FotoAutomovelResult(automovel.Foto!, automovel.FotoTipoConteudo));
    }
}
