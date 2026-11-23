using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloReserva.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloCupom;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCatalogo;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloReserva;

namespace LocadoraDeVeiculos.Aplicacao.ModuloReserva;

/// <summary>Resultado de validar um pedido: o carro, o valor previsto e o nome da locadora.</summary>
public record PreparoReserva(
    CarroReservavel Carro,
    EstimativaReservaDto Estimativa,
    string NomeLocadora,
    string DescricaoVeiculo,
    IReadOnlyList<ReservaTaxa> Taxas,
    Cupom? Cupom
);

/// <summary>
/// Regras do pedido de reserva (lado do cliente). Atravessa as locadoras, então usa leituras que ignoram o filtro por empresa,
/// e calcula o valor com a mesma conta do aluguel (<see cref="Aluguel.CalcularPrevisto"/>).
/// </summary>
public class ReservaServico(
    RepositorioReservaEmOrm repositorioReserva,
    RepositorioCatalogoEmOrm repositorioCatalogo
)
{
    private const int DiasMaximosDeReserva = 365;

    private const int MaximoDeTaxas = 20;

    /// <summary>
    /// Valida o pedido e calcula o valor. <paramref name="usuarioClienteId"/> e <paramref name="cpf"/> servem para conferir se o
    /// cliente já usou o cupom nesta locadora.
    /// </summary>
    public async Task<Result<PreparoReserva>> PrepararAsync(DadosReservaDto dados, Guid? usuarioClienteId = null, string? cpf = null)
    {
        DateOnly hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!Enum.IsDefined(dados.TipoPlano))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O plano de cobrança informado é inválido."));

        if (!Enum.IsDefined(dados.TipoSeguro))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O tipo de seguro informado é inválido."));

        if (dados.Retirada == default || dados.Devolucao == default)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("Informe a retirada e a devolução."));

        // Uma folga de 1 dia: o servidor trabalha em UTC e o cliente pode estar um dia atrás.
        if (dados.Retirada < hoje.AddDays(-1))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A retirada não pode ser no passado."));

        if (dados.Devolucao < dados.Retirada)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A devolução não pode ser antes da retirada."));

        if (dados.Devolucao.DayNumber - dados.Retirada.DayNumber > DiasMaximosDeReserva)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro($"A reserva não pode passar de {DiasMaximosDeReserva} dias."));

        CarroReservavel? carro = await repositorioReserva.SelecionarCarroAsync(dados.AutomovelId);

        if (carro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(dados.AutomovelId));

        if (carro.Plano is null)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                "Este carro ainda não tem plano de cobrança. Escolha outro ou fale com a locadora."));

        decimal precoSeguro = 0m;

        if (dados.TipoSeguro != TipoSeguro.Nenhum)
        {
            precoSeguro = carro.SeguroDiaria;

            if (precoSeguro <= 0)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                    "Este carro não tem seguro disponível. Reserve sem seguro ou fale com a locadora."));
        }

        if (await repositorioReserva.CarroOcupadoAsync(carro.AutomovelId, dados.Retirada, dados.Devolucao, hoje))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("O veículo não está disponível no período informado."));

        // Taxas e serviços: só os do catálogo da locadora do carro.
        List<Guid> idsTaxas = (dados.TaxasIds ?? []).Distinct().ToList();

        if (idsTaxas.Count > MaximoDeTaxas)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro($"Escolha no máximo {MaximoDeTaxas} taxas e serviços."));

        List<TaxaServico> taxasDaLocadora = await repositorioReserva.SelecionarTaxasDaLocadoraAsync(carro.EmpresaId);
        List<TaxaServico> taxas = taxasDaLocadora.Where(t => idsTaxas.Contains(t.Id)).ToList();

        if (taxas.Count != idsTaxas.Count)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("Uma das taxas ou serviços escolhidos não existe nesta locadora."));

        // Cupom: procurado pelo nome na locadora do carro; vale o mesmo que no aluguel (não vencido, um uso por cliente).
        Cupom? cupom = null;

        if (!string.IsNullOrWhiteSpace(dados.Cupom))
        {
            cupom = await repositorioReserva.SelecionarCupomPorNomeAsync(carro.EmpresaId, dados.Cupom);

            if (cupom is null)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("Não encontramos este cupom nesta locadora."));

            if (!cupom.EstaAtivo(hoje))
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro($"O cupom \"{cupom.Nome}\" expirou em {cupom.DataValidade:dd/MM/yyyy}."));

            if (usuarioClienteId is { } usuario &&
                await repositorioReserva.ClienteJaUsouCupomAsync(usuario, carro.EmpresaId, cpf, cupom.Id))
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                    $"Você já usou o cupom \"{cupom.Nome}\" nesta locadora. Cada cupom vale uma vez por cliente."));
        }

        // A mesma conta do aluguel, com um aluguel de mentira só para calcular.
        Aluguel simulado = new()
        {
            DataSaida = dados.Retirada,
            DataRetornoPrevista = dados.Devolucao,
            TipoPlano = dados.TipoPlano,
            TipoSeguro = dados.TipoSeguro,
            SeguroPrecoDiaria = precoSeguro,
            ValorCaucao = Aluguel.ValorCaucaoPadrao,
            Taxas = taxas.Select(t => new AluguelTaxa(t.Id, t.Nome, t.Preco, t.TipoCobranca)).ToList(),
            CupomId = cupom?.Id,
            CupomNome = cupom?.Nome,
            CupomTipo = cupom?.Tipo ?? TipoDesconto.Valor,
            CupomValor = cupom?.Valor ?? 0m
        };

        switch (dados.TipoPlano)
        {
            case TipoPlano.Diario:
                simulado.PlanoPrecoDiaria = carro.Plano.DiarioPrecoDiaria;
                break;

            case TipoPlano.Controlado:
                simulado.PlanoPrecoDiaria = carro.Plano.ControladoPrecoDiaria;
                break;

            default:
                simulado.PlanoPrecoDiaria = carro.Plano.LivrePrecoDiaria;
                break;
        }

        DetalhamentoPrevisto previsto = simulado.CalcularPrevisto();

        var linhasDeTaxas = simulado.Taxas
            .Select(t => new TaxaDaReservaDto(t.TaxaServicoId, t.Nome, t.PrecoUnitario, t.TipoCobranca, t.ValorPara(previsto.Dias)))
            .ToList();

        var estimativa = new EstimativaReservaDto(
            previsto.Dias, previsto.ValorDiarias, previsto.ValorSeguro, previsto.ValorCaucao, previsto.ValorTotalPrevisto,
            previsto.ValorTaxas, previsto.ValorDesconto, linhasDeTaxas, cupom?.Nome, cupom?.Tipo, cupom?.Valor ?? 0m);

        List<ReservaTaxa> taxasDaReserva = taxas
            .Select(t => new ReservaTaxa { TaxaServicoId = t.Id, Nome = t.Nome, PrecoUnitario = t.Preco, TipoCobranca = t.TipoCobranca })
            .ToList();

        string nomeLocadora = await repositorioCatalogo.SelecionarNomeDaLocadoraAsync(carro.EmpresaId);

        return Result.Ok(new PreparoReserva(
            carro, estimativa, nomeLocadora, $"{carro.Marca} {carro.Modelo} ({carro.Ano})", taxasDaReserva, cupom));
    }
}
