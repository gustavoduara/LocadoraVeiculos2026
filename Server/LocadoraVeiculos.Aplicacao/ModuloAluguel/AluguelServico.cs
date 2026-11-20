using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Validators;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAutomovel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConfiguracao;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloTaxaServico;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAluguel;

/// <summary>Dados já validados para registrar a devolução de um aluguel.</summary>
public record DevolucaoPreparada(Aluguel Aluguel, decimal PrecoCombustivelPorLitro, int CapacidadeTanqueLitros);

/// <summary>
/// Regras de negócio do aluguel compartilhadas entre cadastro, edição, simulação de preço e devolução.
/// Monta o aluguel "congelando" os preços atuais de plano, seguro e taxas.
/// </summary>
public class AluguelServico(
    RepositorioCondutorEmOrm repositorioCondutor,
    RepositorioAutomovelEmOrm repositorioAutomovel,
    RepositorioPlanoCobrancaEmOrm repositorioPlano,
    RepositorioTaxaServicoEmOrm repositorioTaxa,
    RepositorioConfiguracaoEmOrm repositorioConfiguracao,
    RepositorioAluguelEmOrm repositorioAluguel,
    ITenantProvider tenantProvider
)
{
    /// <summary>
    /// Valida os dados e devolve um aluguel (ainda não gravado) com todos os valores calculados.
    /// Em <paramref name="apenasPreco"/> (simulação) ignora as regras que não afetam o preço:
    /// condutor, documentos e disponibilidade do veículo.
    /// </summary>
    public async Task<Result<Aluguel>> MontarAsync(
        DadosAluguelDto dados, bool apenasPreco, Guid? ignorarAluguelId = null)
    {
        var validacao = new DadosAluguelValidator().Validate(dados);

        if (!validacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(validacao.Errors.Select(e => e.ErrorMessage)));

        // O filtro global por empresa garante que só registros da própria locadora são encontrados.
        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(dados.AutomovelId);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(dados.AutomovelId));

        PlanoCobranca? plano = await repositorioPlano.SelecionarPorGrupoAsync(automovel.GrupoVeiculosId);

        if (plano is null)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                "O grupo deste veículo ainda não possui plano de cobrança cadastrado."));

        List<Guid> idsTaxas = dados.TaxasIds.Distinct().ToList();
        List<TaxaServico> taxas = await repositorioTaxa.SelecionarPorIdsAsync(idsTaxas);

        if (taxas.Count != idsTaxas.Count)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Taxa ou serviço informado."));

        Configuracao? configuracao = await repositorioConfiguracao.SelecionarDaEmpresaAsync();

        decimal precoSeguro = 0m;

        if (dados.TipoSeguro != TipoSeguro.Nenhum)
        {
            precoSeguro = configuracao?.ObterPrecoSeguro(dados.TipoSeguro) ?? 0m;

            if (precoSeguro <= 0)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                    "O valor diário do seguro escolhido ainda não foi configurado. Defina-o em Configurações."));
        }

        Condutor? condutor = null;

        if (!apenasPreco)
        {
            condutor = await repositorioCondutor.SelecionarPorIdAsync(dados.CondutorId);

            if (condutor is null)
                return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(dados.CondutorId));

            // Documentos em dia: a CNH precisa valer durante todo o aluguel.
            if (condutor.ValidadeCnh < dados.DataRetornoPrevista)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                    $"A CNH do condutor vence em {condutor.ValidadeCnh:dd/MM/yyyy}, antes do retorno previsto do aluguel."));

            DateOnly hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            if (await repositorioAluguel.ExisteConflitoDeDisponibilidadeAsync(
                    automovel.Id, dados.DataSaida, dados.DataRetornoPrevista, hoje, ignorarAluguelId))
                return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                    "O veículo não está disponível no período informado."));
        }

        Aluguel aluguel = new()
        {
            EmpresaId = tenantProvider.EmpresaId.GetValueOrDefault(),
            CondutorId = dados.CondutorId,
            ClienteId = condutor?.ClienteId ?? Guid.Empty,
            AutomovelId = automovel.Id,
            PlanoCobrancaId = plano.Id,
            NomeCondutor = condutor?.Nome ?? string.Empty,
            NomeCliente = condutor?.Cliente?.Nome ?? string.Empty,
            DescricaoVeiculo = $"{automovel.Marca} {automovel.Modelo} ({automovel.Placa})",
            NomeGrupo = automovel.GrupoVeiculos?.Nome ?? string.Empty,
            DataSaida = dados.DataSaida,
            DataRetornoPrevista = dados.DataRetornoPrevista,
            KmInicial = dados.KmInicial,
            TipoPlano = dados.TipoPlano,
            TipoSeguro = dados.TipoSeguro,
            SeguroPrecoDiaria = precoSeguro,
            ValorCaucao = Aluguel.ValorCaucaoPadrao,
            Taxas = taxas
                .Select(t => new AluguelTaxa(t.Id, t.Nome, t.Preco, t.TipoCobranca))
                .ToList()
        };

        // Valores do plano escolhido, copiados do plano de cobrança do grupo do veículo.
        switch (dados.TipoPlano)
        {
            case TipoPlano.Diario:
                aluguel.PlanoPrecoDiaria = plano.DiarioPrecoDiaria;
                aluguel.PlanoPrecoPorKm = plano.DiarioPrecoPorKm;
                break;

            case TipoPlano.Controlado:
                aluguel.PlanoPrecoDiaria = plano.ControladoPrecoDiaria;
                aluguel.PlanoPrecoPorKm = plano.ControladoPrecoPorKmExtrapolado;
                aluguel.PlanoKmDisponiveisPorDia = plano.ControladoKmDisponiveis;
                break;

            default:
                aluguel.PlanoPrecoDiaria = plano.LivrePrecoDiaria;
                break;
        }

        return Result.Ok(aluguel);
    }

    /// <summary>
    /// Confere se o aluguel pode ser devolvido com os dados informados e reúne o que o cálculo
    /// precisa (preço do combustível e capacidade do tanque, quando o tanque não está cheio).
    /// </summary>
    public async Task<Result<DevolucaoPreparada>> PrepararDevolucaoAsync(
        Guid aluguelId, DateOnly dataDevolucao, int kmFinal, bool tanqueCheio)
    {
        var validacao = new DadosDevolucaoValidator().Validate(
            new DevolverAluguelCommand(aluguelId, dataDevolucao, kmFinal, tanqueCheio));

        if (!validacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(validacao.Errors.Select(e => e.ErrorMessage)));

        Aluguel? aluguel = await repositorioAluguel.SelecionarPorIdAsync(aluguelId);

        if (aluguel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(aluguelId));

        if (!aluguel.EstaEmAberto)
            return Result.Fail(ResultadosErro.OperacaoBloqueadaErro("Este aluguel já foi concluído."));

        if (dataDevolucao < aluguel.DataSaida)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                "A data de devolução não pode ser anterior à data de saída."));

        if (kmFinal < aluguel.KmInicial)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                $"A quilometragem atual não pode ser menor que a inicial ({aluguel.KmInicial} km)."));

        Automovel? automovel = await repositorioAutomovel.SelecionarPorIdAsync(aluguel.AutomovelId);

        if (automovel is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(aluguel.AutomovelId));

        decimal precoCombustivel = 0m;

        if (!tanqueCheio)
        {
            Configuracao? configuracao = await repositorioConfiguracao.SelecionarDaEmpresaAsync();

            precoCombustivel = configuracao?.ObterPreco(automovel.TipoCombustivel) ?? 0m;

            if (precoCombustivel <= 0)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                    $"O preço do combustível ({automovel.TipoCombustivel}) ainda não foi configurado. Defina-o em Configurações."));
        }

        return Result.Ok(new DevolucaoPreparada(aluguel, precoCombustivel, automovel.CapacidadeCombustivelLitros));
    }
}
