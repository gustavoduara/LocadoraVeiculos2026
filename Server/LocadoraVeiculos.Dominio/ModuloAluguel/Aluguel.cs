using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Dominio.ModuloFuncionario;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

namespace LocadoraDeVeiculos.Dominio.ModuloAluguel;

/// <summary>Valores previstos no momento do cadastro (km, combustível e multa só existem na devolução).</summary>
public record DetalhamentoPrevisto(
    int Dias,
    decimal ValorCaucao,
    decimal ValorDiarias,
    decimal ValorTaxas,
    decimal ValorSeguro,
    decimal ValorDesconto,
    decimal ValorTotalPrevisto
);

/// <summary>Valores finais, calculados na devolução.</summary>
public record DetalhamentoFinal(
    int Dias,
    int KmRodados,
    bool Atrasado,
    decimal ValorCaucao,
    decimal ValorDiarias,
    decimal ValorKm,
    decimal ValorTaxas,
    decimal ValorSeguro,
    decimal ValorCombustivel,
    decimal ValorMulta,
    decimal ValorDesconto,
    decimal ValorFinal
);

/// <summary>
/// Aluguel de um automóvel para um cliente, conduzido por um condutor.
/// Os valores de plano, seguro e taxas são copiados para o aluguel ao registrá-lo (ficam "congelados"),
/// de modo que mudanças posteriores em preços não alteram aluguéis já feitos.
/// </summary>
public class Aluguel : EntidadeBase<Aluguel>
{
    /// <summary>Garantia/caução fixa definida pela especificação.</summary>
    public const decimal ValorCaucaoPadrao = 1000m;

    /// <summary>Multa por atraso na devolução, sobre o valor do aluguel.</summary>
    public const decimal PercentualMulta = 0.10m;

    // ---- vínculos
    public Guid CondutorId { get; set; }
    public Condutor? Condutor { get; set; }

    public Guid ClienteId { get; set; }

    public Guid AutomovelId { get; set; }
    public Automovel? Automovel { get; set; }

    public Guid PlanoCobrancaId { get; set; }
    public PlanoCobranca? PlanoCobranca { get; set; }

    /// <summary>Funcionário que registrou o aluguel (vazio quando foi o administrador da empresa).</summary>
    public Guid? FuncionarioId { get; set; }
    public Funcionario? Funcionario { get; set; }

    // ---- dados para exibição (cópias, para o histórico sobreviver a mudanças nos cadastros)
    public string NomeCondutor { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string DescricaoVeiculo { get; set; } = string.Empty;
    public string NomeGrupo { get; set; } = string.Empty;

    // ---- período e veículo
    public DateOnly DataSaida { get; set; }
    public DateOnly DataRetornoPrevista { get; set; }
    public int KmInicial { get; set; }

    // ---- plano escolhido (valores copiados do plano de cobrança)
    public TipoPlano TipoPlano { get; set; }
    public decimal PlanoPrecoDiaria { get; set; }

    /// <summary>Plano Diário: preço por km rodado. Plano Controlado: preço por km extrapolado. Livre: 0.</summary>
    public decimal PlanoPrecoPorKm { get; set; }

    /// <summary>Somente Plano Controlado: km incluídos por dia de aluguel.</summary>
    public int PlanoKmDisponiveisPorDia { get; set; }

    // ---- seguro e caução
    public TipoSeguro TipoSeguro { get; set; }
    public decimal SeguroPrecoDiaria { get; set; }
    public decimal ValorCaucao { get; set; } = ValorCaucaoPadrao;

    public List<AluguelTaxa> Taxas { get; set; } = [];

    // ---- cupom de desconto (valor copiado do cupom no momento do registro)
    public Guid? CupomId { get; set; }
    public string? CupomNome { get; set; }
    public decimal CupomValor { get; set; }

    /// <summary>Se <see cref="CupomValor"/> é um valor em reais ou uma porcentagem.</summary>
    public TipoDesconto CupomTipo { get; set; } = TipoDesconto.Valor;

    // ---- situação e devolução
    public StatusAluguel Status { get; set; } = StatusAluguel.EmAberto;
    public DateOnly? DataDevolucao { get; set; }
    public int? KmFinal { get; set; }
    public bool? TanqueCheio { get; set; }

    /// <summary>Preço do litro e capacidade do tanque usados na devolução (só quando o tanque não está cheio).</summary>
    public decimal? PrecoCombustivelPorLitro { get; set; }
    public int? CapacidadeTanqueLitros { get; set; }

    public Aluguel() { }

    public bool EstaEmAberto => Status == StatusAluguel.EmAberto;

    // ================= cálculo =================

    /// <summary>Dias de aluguel: a diferença entre as datas, com mínimo de 1 diária.</summary>
    public static int CalcularDias(DateOnly saida, DateOnly retorno) =>
        Math.Max(1, retorno.DayNumber - saida.DayNumber);

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public int Dias => CalcularDias(DataSaida, DataRetornoPrevista);

    public decimal ValorDiarias => Arredondar(PlanoPrecoDiaria * Dias);

    public decimal ValorTaxas => Arredondar(Taxas.Sum(t => t.ValorPara(Dias)));

    public decimal ValorSeguro => Arredondar(SeguroPrecoDiaria * Dias);

    /// <summary>
    /// Desconto do cupom, limitado ao que foi consumido: um valor fixo ou uma porcentagem do consumo
    /// (a caução não recebe desconto, pois é devolvida ao cliente).
    /// </summary>
    private decimal CalcularDesconto(decimal consumo) =>
        CupomTipo == TipoDesconto.Percentual
            ? Math.Min(Arredondar(consumo * CupomValor / 100m), consumo)
            : Math.Min(CupomValor, consumo);

    /// <summary>Caução + diárias do plano + taxas + seguro − desconto. Km rodados, combustível e multa só entram na devolução.</summary>
    public DetalhamentoPrevisto CalcularPrevisto()
    {
        decimal consumo = ValorDiarias + ValorTaxas + ValorSeguro;
        decimal desconto = CalcularDesconto(consumo);

        return new(
            Dias,
            ValorCaucao,
            ValorDiarias,
            ValorTaxas,
            ValorSeguro,
            desconto,
            ValorCaucao + consumo - desconto
        );
    }

    /// <summary>
    /// Simula a devolução sem alterar o aluguel.
    /// Plano Diário cobra cada km rodado; Plano Controlado cobra apenas os km além da franquia
    /// (km por dia × dias); Plano Livre não cobra km. Se o tanque não estiver cheio, cobra-se o
    /// reabastecimento completo. Atraso gera multa de 10% sobre diárias, km, taxas e seguro.
    /// </summary>
    public DetalhamentoFinal CalcularFinal(
        DateOnly dataDevolucao,
        int kmFinal,
        bool tanqueCheio,
        decimal precoCombustivelPorLitro,
        int capacidadeTanqueLitros)
    {
        int kmRodados = Math.Max(0, kmFinal - KmInicial);

        decimal valorKm = TipoPlano switch
        {
            TipoPlano.Diario => kmRodados * PlanoPrecoPorKm,
            TipoPlano.Controlado => Math.Max(0, kmRodados - PlanoKmDisponiveisPorDia * Dias) * PlanoPrecoPorKm,
            _ => 0m
        };

        valorKm = Arredondar(valorKm);

        decimal consumo = ValorDiarias + valorKm + ValorTaxas + ValorSeguro;

        bool atrasado = dataDevolucao > DataRetornoPrevista;
        decimal multa = atrasado ? Arredondar(consumo * PercentualMulta) : 0m;

        decimal combustivel = tanqueCheio
            ? 0m
            : Arredondar(capacidadeTanqueLitros * precoCombustivelPorLitro);

        decimal desconto = CalcularDesconto(consumo);

        return new DetalhamentoFinal(
            Dias,
            kmRodados,
            atrasado,
            ValorCaucao,
            ValorDiarias,
            valorKm,
            ValorTaxas,
            ValorSeguro,
            combustivel,
            multa,
            desconto,
            ValorCaucao + consumo - desconto + combustivel + multa
        );
    }

    /// <summary>Detalhamento final de um aluguel já concluído, refeito a partir dos dados gravados.</summary>
    public DetalhamentoFinal? ObterFinal()
    {
        if (Status != StatusAluguel.Concluido || DataDevolucao is null || KmFinal is null || TanqueCheio is null)
            return null;

        return CalcularFinal(
            DataDevolucao.Value,
            KmFinal.Value,
            TanqueCheio.Value,
            PrecoCombustivelPorLitro ?? 0m,
            CapacidadeTanqueLitros ?? 0
        );
    }

    // ================= comportamento =================

    /// <summary>Registra a devolução e conclui o aluguel. As validações ficam a cargo da camada de aplicação.</summary>
    public void Devolver(
        DateOnly dataDevolucao,
        int kmFinal,
        bool tanqueCheio,
        decimal precoCombustivelPorLitro,
        int capacidadeTanqueLitros)
    {
        DataDevolucao = dataDevolucao;
        KmFinal = kmFinal;
        TanqueCheio = tanqueCheio;
        PrecoCombustivelPorLitro = tanqueCheio ? null : precoCombustivelPorLitro;
        CapacidadeTanqueLitros = tanqueCheio ? null : capacidadeTanqueLitros;
        Status = StatusAluguel.Concluido;
    }

    /// <summary>Copia os dados editáveis de outro aluguel (montado com os preços atuais), preservando identidade e autoria.</summary>
    public override void AtualizarRegistro(Aluguel editado)
    {
        CondutorId = editado.CondutorId;
        ClienteId = editado.ClienteId;
        AutomovelId = editado.AutomovelId;
        PlanoCobrancaId = editado.PlanoCobrancaId;

        NomeCondutor = editado.NomeCondutor;
        NomeCliente = editado.NomeCliente;
        DescricaoVeiculo = editado.DescricaoVeiculo;
        NomeGrupo = editado.NomeGrupo;

        DataSaida = editado.DataSaida;
        DataRetornoPrevista = editado.DataRetornoPrevista;
        KmInicial = editado.KmInicial;

        TipoPlano = editado.TipoPlano;
        PlanoPrecoDiaria = editado.PlanoPrecoDiaria;
        PlanoPrecoPorKm = editado.PlanoPrecoPorKm;
        PlanoKmDisponiveisPorDia = editado.PlanoKmDisponiveisPorDia;

        TipoSeguro = editado.TipoSeguro;
        SeguroPrecoDiaria = editado.SeguroPrecoDiaria;

        CupomId = editado.CupomId;
        CupomNome = editado.CupomNome;
        CupomValor = editado.CupomValor;
        CupomTipo = editado.CupomTipo;

        Taxas.Clear();

        foreach (AluguelTaxa taxa in editado.Taxas)
            Taxas.Add(new AluguelTaxa(taxa.TaxaServicoId, taxa.Nome, taxa.PrecoUnitario, taxa.TipoCobranca));
    }
}
