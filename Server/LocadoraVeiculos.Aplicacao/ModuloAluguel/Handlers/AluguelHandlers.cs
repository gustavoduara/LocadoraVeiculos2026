using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloFuncionario;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloFuncionario;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Handlers;

internal static class AluguelMapeamento
{
    public static IReadOnlyList<AluguelTaxaDto> TaxasParaDto(this Aluguel a) =>
        a.Taxas
            .Select(t => new AluguelTaxaDto(
                t.TaxaServicoId,
                t.Nome,
                t.PrecoUnitario,
                t.TipoCobranca,
                Math.Round(t.ValorPara(a.Dias), 2, MidpointRounding.AwayFromZero)))
            .ToList();

    public static AluguelDto ParaDto(this Aluguel a)
    {
        DetalhamentoFinal? final = a.ObterFinal();

        DevolucaoDto? devolucao = final is null
            ? null
            : new DevolucaoDto(a.DataDevolucao!.Value, a.KmFinal!.Value, a.TanqueCheio!.Value, final);

        return new AluguelDto(
            a.Id,
            a.CondutorId,
            a.NomeCondutor,
            a.ClienteId,
            a.NomeCliente,
            a.AutomovelId,
            a.DescricaoVeiculo,
            a.NomeGrupo,
            a.TipoPlano,
            a.DataSaida,
            a.DataRetornoPrevista,
            a.KmInicial,
            a.TipoSeguro,
            a.TaxasParaDto(),
            a.Status,
            a.CalcularPrevisto(),
            devolucao,
            a.CupomId,
            a.CupomNome,
            a.CupomId is null ? null : a.CupomTipo,
            a.CupomValor
        );
    }
}

public class CadastrarAluguelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAluguelEmOrm repositorioAluguel,
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    AluguelServico servico,
    ITenantProvider tenantProvider,
    ILogger<CadastrarAluguelCommandHandler> logger
) : IRequestHandler<CadastrarAluguelCommand, Result<CadastrarAluguelResult>>
{
    public async Task<Result<CadastrarAluguelResult>> Handle(
        CadastrarAluguelCommand command, CancellationToken cancellationToken)
    {
        Result<Aluguel> montagem = await servico.MontarAsync(command.Dados, apenasPreco: false);

        if (montagem.IsFailed)
            return Result.Fail(montagem.Errors);

        Aluguel aluguel = montagem.Value;

        try
        {
            // Guarda quem registrou, quando for um funcionário (a regra de exclusão de funcionário usa isso).
            if (tenantProvider.EstaNoCargo(CargoUsuario.Funcionario.ToString()) && tenantProvider.UsuarioId is not null)
            {
                Funcionario? funcionario = await repositorioFuncionario.SelecionarPorUsuarioIdAsync(tenantProvider.UsuarioId.Value);

                aluguel.FuncionarioId = funcionario?.Id;
            }

            await repositorioAluguel.CadastrarAsync(aluguel);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarAluguelResult(aluguel.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarAluguelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAluguelEmOrm repositorioAluguel,
    AluguelServico servico,
    ILogger<EditarAluguelCommandHandler> logger
) : IRequestHandler<EditarAluguelCommand, Result<EditarAluguelResult>>
{
    public async Task<Result<EditarAluguelResult>> Handle(
        EditarAluguelCommand command, CancellationToken cancellationToken)
    {
        Aluguel? aluguel = await repositorioAluguel.SelecionarPorIdAsync(command.Id);

        if (aluguel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (!aluguel.EstaEmAberto)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Não é possível editar um aluguel já concluído."));

        // Ao editar, os valores são recalculados com os preços vigentes hoje.
        Result<Aluguel> montagem = await servico.MontarAsync(command.Dados, apenasPreco: false, ignorarAluguelId: aluguel.Id);

        if (montagem.IsFailed)
            return Result.Fail(montagem.Errors);

        try
        {
            var taxasAnteriores = aluguel.Taxas.Select(t => t.Id).ToHashSet();

            aluguel.AtualizarRegistro(montagem.Value);

            // Ids já nascem preenchidos: sem isto o EF trataria as novas taxas como existentes (UPDATE).
            foreach (var taxa in aluguel.Taxas.Where(t => !taxasAnteriores.Contains(t.Id)))
                appDbContext.Entry(taxa).State = Microsoft.EntityFrameworkCore.EntityState.Added;

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarAluguelResult(aluguel.ParaDto()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirAluguelCommandHandler(
    AppDbContext appDbContext,
    RepositorioAluguelEmOrm repositorioAluguel,
    ILogger<ExcluirAluguelCommandHandler> logger
) : IRequestHandler<ExcluirAluguelCommand, Result<ExcluirAluguelResult>>
{
    public async Task<Result<ExcluirAluguelResult>> Handle(
        ExcluirAluguelCommand command, CancellationToken cancellationToken)
    {
        Aluguel? aluguel = await repositorioAluguel.SelecionarPorIdAsync(command.Id);

        if (aluguel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (aluguel.EstaEmAberto)
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir um aluguel ainda não concluído. Registre a devolução antes."));

        try
        {
            await repositorioAluguel.ExcluirAsync(aluguel.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirAluguelResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarAluguelPorIdQueryHandler(
    RepositorioAluguelEmOrm repositorioAluguel
) : IRequestHandler<SelecionarAluguelPorIdQuery, Result<AluguelDto>>
{
    public async Task<Result<AluguelDto>> Handle(
        SelecionarAluguelPorIdQuery query, CancellationToken cancellationToken)
    {
        Aluguel? aluguel = await repositorioAluguel.SelecionarPorIdAsync(query.Id);

        if (aluguel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(aluguel.ParaDto());
    }
}

public class SelecionarAlugueisQueryHandler(
    RepositorioAluguelEmOrm repositorioAluguel
) : IRequestHandler<SelecionarAlugueisQuery, Result<SelecionarAlugueisResult>>
{
    public async Task<Result<SelecionarAlugueisResult>> Handle(
        SelecionarAlugueisQuery query, CancellationToken cancellationToken)
    {
        List<Aluguel> alugueis = await repositorioAluguel.SelecionarTodosAsync(query.Status);

        return Result.Ok(new SelecionarAlugueisResult(alugueis.Select(a => a.ParaDto()).ToList()));
    }
}

public class CalcularAluguelQueryHandler(
    AluguelServico servico
) : IRequestHandler<CalcularAluguelQuery, Result<SimulacaoAluguelDto>>
{
    public async Task<Result<SimulacaoAluguelDto>> Handle(
        CalcularAluguelQuery query, CancellationToken cancellationToken)
    {
        Result<Aluguel> montagem = await servico.MontarAsync(query.Dados, apenasPreco: true);

        if (montagem.IsFailed)
            return Result.Fail(montagem.Errors);

        Aluguel aluguel = montagem.Value;

        return Result.Ok(new SimulacaoAluguelDto(aluguel.TaxasParaDto(), aluguel.CalcularPrevisto()));
    }
}

public class DevolverAluguelCommandHandler(
    AppDbContext appDbContext,
    AluguelServico servico,
    ILogger<DevolverAluguelCommandHandler> logger
) : IRequestHandler<DevolverAluguelCommand, Result<DevolverAluguelResult>>
{
    public async Task<Result<DevolverAluguelResult>> Handle(
        DevolverAluguelCommand command, CancellationToken cancellationToken)
    {
        Result<DevolucaoPreparada> preparo = await servico.PrepararDevolucaoAsync(
            command.Id, command.DataDevolucao, command.KmFinal, command.TanqueCheio);

        if (preparo.IsFailed)
            return Result.Fail(preparo.Errors);

        Aluguel aluguel = preparo.Value.Aluguel;

        try
        {
            aluguel.Devolver(
                command.DataDevolucao,
                command.KmFinal,
                command.TanqueCheio,
                preparo.Value.PrecoCombustivelPorLitro,
                preparo.Value.CapacidadeTanqueLitros
            );

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new DevolverAluguelResult(aluguel.ParaDto()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a devolução de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class CalcularDevolucaoQueryHandler(
    AluguelServico servico
) : IRequestHandler<CalcularDevolucaoQuery, Result<DetalhamentoFinal>>
{
    public async Task<Result<DetalhamentoFinal>> Handle(
        CalcularDevolucaoQuery query, CancellationToken cancellationToken)
    {
        Result<DevolucaoPreparada> preparo = await servico.PrepararDevolucaoAsync(
            query.Id, query.DataDevolucao, query.KmFinal, query.TanqueCheio);

        if (preparo.IsFailed)
            return Result.Fail(preparo.Errors);

        DevolucaoPreparada p = preparo.Value;

        return Result.Ok(p.Aluguel.CalcularFinal(
            query.DataDevolucao,
            query.KmFinal,
            query.TanqueCheio,
            p.PrecoCombustivelPorLitro,
            p.CapacidadeTanqueLitros
        ));
    }
}
