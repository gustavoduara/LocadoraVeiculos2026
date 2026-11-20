using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloReserva.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCatalogo;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloReserva;

namespace LocadoraDeVeiculos.Aplicacao.ModuloReserva;

/// <summary>Resultado de validar um pedido: o carro, o valor previsto e o nome da locadora.</summary>
public record PreparoReserva(CarroReservavel Carro, EstimativaReservaDto Estimativa, string NomeLocadora, string DescricaoVeiculo);

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

    public async Task<Result<PreparoReserva>> PrepararAsync(DadosReservaDto dados)
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
            precoSeguro = carro.Configuracao?.ObterPrecoSeguro(dados.TipoSeguro) ?? 0m;

            if (precoSeguro <= 0)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                    "Esta locadora ainda não oferece o seguro escolhido. Escolha outra opção."));
        }

        if (await repositorioReserva.CarroOcupadoAsync(carro.AutomovelId, dados.Retirada, dados.Devolucao, hoje))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("O veículo não está disponível no período informado."));

        // A mesma conta do aluguel, com um aluguel de mentira só para calcular.
        Aluguel simulado = new()
        {
            DataSaida = dados.Retirada,
            DataRetornoPrevista = dados.Devolucao,
            TipoPlano = dados.TipoPlano,
            TipoSeguro = dados.TipoSeguro,
            SeguroPrecoDiaria = precoSeguro,
            ValorCaucao = Aluguel.ValorCaucaoPadrao
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

        var estimativa = new EstimativaReservaDto(
            previsto.Dias, previsto.ValorDiarias, previsto.ValorSeguro, previsto.ValorCaucao, previsto.ValorTotalPrevisto);

        string nomeLocadora = await repositorioCatalogo.SelecionarNomeDaLocadoraAsync(carro.EmpresaId);

        return Result.Ok(new PreparoReserva(carro, estimativa, nomeLocadora, $"{carro.Marca} {carro.Modelo} ({carro.Ano})"));
    }
}
