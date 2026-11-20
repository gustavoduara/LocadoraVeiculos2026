using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

namespace LocadoraDeVeiculos.Testes.Dominio;

public class AluguelTestes
{
    private static readonly DateOnly Saida = new(2026, 11, 1);

    /// <summary>Aluguel de 3 dias, plano Diário (R$ 150/dia, R$ 1,50/km), seguro R$ 30/dia, GPS diário + cadeirinha fixa.</summary>
    private static Aluguel Criar(
        TipoPlano plano = TipoPlano.Diario,
        int dias = 3,
        decimal seguro = 30m,
        bool comTaxas = true)
    {
        var aluguel = new Aluguel
        {
            DataSaida = Saida,
            DataRetornoPrevista = Saida.AddDays(dias),
            KmInicial = 1000,
            TipoPlano = plano,
            PlanoPrecoDiaria = plano switch
            {
                TipoPlano.Diario => 150m,
                TipoPlano.Controlado => 200m,
                _ => 300m
            },
            PlanoPrecoPorKm = plano switch
            {
                TipoPlano.Diario => 1.5m,
                TipoPlano.Controlado => 2.5m,
                _ => 0m
            },
            PlanoKmDisponiveisPorDia = plano == TipoPlano.Controlado ? 100 : 0,
            TipoSeguro = seguro > 0 ? TipoSeguro.Cliente : TipoSeguro.Nenhum,
            SeguroPrecoDiaria = seguro
        };

        if (comTaxas)
        {
            aluguel.Taxas.Add(new AluguelTaxa(Guid.NewGuid(), "GPS", 20m, TipoCobrancaTaxa.Diario));
            aluguel.Taxas.Add(new AluguelTaxa(Guid.NewGuid(), "Cadeirinha", 50m, TipoCobrancaTaxa.Fixo));
        }

        return aluguel;
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(30, 30)]
    public void CalcularDias_cobra_no_minimo_uma_diaria(int diferenca, int esperado)
    {
        Assert.Equal(esperado, Aluguel.CalcularDias(Saida, Saida.AddDays(diferenca)));
    }

    [Fact]
    public void Previsto_soma_caucao_diarias_taxas_e_seguro()
    {
        var previsto = Criar().CalcularPrevisto();

        Assert.Equal(3, previsto.Dias);
        Assert.Equal(1000m, previsto.ValorCaucao);
        Assert.Equal(450m, previsto.ValorDiarias);
        Assert.Equal(110m, previsto.ValorTaxas); // GPS 20 x 3 + cadeirinha 50
        Assert.Equal(90m, previsto.ValorSeguro);
        Assert.Equal(1650m, previsto.ValorTotalPrevisto);
    }

    [Fact]
    public void Previsto_sem_taxas_nem_seguro_tem_so_caucao_e_diarias()
    {
        var previsto = Criar(TipoPlano.Livre, dias: 1, seguro: 0m, comTaxas: false).CalcularPrevisto();

        Assert.Equal(1300m, previsto.ValorTotalPrevisto);
    }

    [Fact]
    public void Plano_diario_cobra_cada_km_rodado()
    {
        var final = Criar().CalcularFinal(Saida.AddDays(3), kmFinal: 1200, tanqueCheio: true, 0m, 0);

        Assert.Equal(200, final.KmRodados);
        Assert.Equal(300m, final.ValorKm);
        Assert.Equal(1950m, final.ValorFinal); // 1000 + 450 + 300 + 110 + 90
        Assert.False(final.Atrasado);
        Assert.Equal(0m, final.ValorMulta);
    }

    [Fact]
    public void Plano_controlado_cobra_apenas_os_km_acima_da_franquia()
    {
        var aluguel = Criar(TipoPlano.Controlado, dias: 2, seguro: 0m, comTaxas: false);

        // Franquia: 100 km x 2 dias = 200. Rodou 300 -> 100 km extras x 2,50.
        var final = aluguel.CalcularFinal(Saida.AddDays(2), 1300, true, 0m, 0);

        Assert.Equal(250m, final.ValorKm);
    }

    [Fact]
    public void Plano_controlado_dentro_da_franquia_nao_cobra_km()
    {
        var aluguel = Criar(TipoPlano.Controlado, dias: 2, seguro: 0m, comTaxas: false);

        var final = aluguel.CalcularFinal(Saida.AddDays(2), 1150, true, 0m, 0);

        Assert.Equal(0m, final.ValorKm);
    }

    [Fact]
    public void Plano_livre_nunca_cobra_km()
    {
        var aluguel = Criar(TipoPlano.Livre, dias: 2, seguro: 0m, comTaxas: false);

        var final = aluguel.CalcularFinal(Saida.AddDays(2), 9000, true, 0m, 0);

        Assert.Equal(0m, final.ValorKm);
        Assert.Equal(1600m, final.ValorFinal); // 1000 + 2 x 300
    }

    [Fact]
    public void Tanque_nao_cheio_cobra_a_capacidade_inteira_pelo_preco_do_litro()
    {
        var final = Criar(seguro: 0m, comTaxas: false).CalcularFinal(Saida.AddDays(3), 1000, false, 5.5m, 60);

        Assert.Equal(330m, final.ValorCombustivel);
    }

    [Fact]
    public void Tanque_cheio_nao_cobra_combustivel()
    {
        var final = Criar().CalcularFinal(Saida.AddDays(3), 1000, true, 5.5m, 60);

        Assert.Equal(0m, final.ValorCombustivel);
    }

    [Fact]
    public void Atraso_gera_multa_de_dez_por_cento_sobre_o_consumo_sem_a_caucao()
    {
        var aluguel = Criar(TipoPlano.Controlado, dias: 2, seguro: 0m, comTaxas: false);

        // diárias 400 + km extras 250 = 650 -> multa 65.
        var final = aluguel.CalcularFinal(Saida.AddDays(3), 1300, true, 0m, 0);

        Assert.True(final.Atrasado);
        Assert.Equal(65m, final.ValorMulta);
        Assert.Equal(1715m, final.ValorFinal); // 1000 + 650 + 65
    }

    [Fact]
    public void Devolver_no_ultimo_dia_previsto_nao_e_atraso()
    {
        var final = Criar().CalcularFinal(Saida.AddDays(3), 1000, true, 0m, 0);

        Assert.False(final.Atrasado);
    }

    [Fact]
    public void Km_final_menor_que_o_inicial_nao_gera_km_negativo()
    {
        var final = Criar().CalcularFinal(Saida.AddDays(3), 500, true, 0m, 0);

        Assert.Equal(0, final.KmRodados);
        Assert.Equal(0m, final.ValorKm);
    }

    [Fact]
    public void Devolver_conclui_o_aluguel_e_guarda_os_dados()
    {
        var aluguel = Criar();

        Assert.True(aluguel.EstaEmAberto);
        Assert.Null(aluguel.ObterFinal());

        aluguel.Devolver(Saida.AddDays(3), 1200, tanqueCheio: false, 5m, 50);

        Assert.Equal(StatusAluguel.Concluido, aluguel.Status);
        Assert.False(aluguel.EstaEmAberto);
        Assert.Equal(5m, aluguel.PrecoCombustivelPorLitro);
        Assert.Equal(50, aluguel.CapacidadeTanqueLitros);

        var final = aluguel.ObterFinal();

        Assert.NotNull(final);
        Assert.Equal(250m, final!.ValorCombustivel);
        Assert.Equal(200, final.KmRodados);
    }

    [Fact]
    public void Devolver_com_tanque_cheio_nao_guarda_preco_de_combustivel()
    {
        var aluguel = Criar();

        aluguel.Devolver(Saida.AddDays(3), 1200, tanqueCheio: true, 5m, 50);

        Assert.Null(aluguel.PrecoCombustivelPorLitro);
        Assert.Null(aluguel.CapacidadeTanqueLitros);
    }

    [Fact]
    public void AtualizarRegistro_substitui_dados_e_taxas_preservando_identidade()
    {
        var aluguel = Criar();
        var id = aluguel.Id;

        var editado = Criar(TipoPlano.Livre, dias: 5, seguro: 0m, comTaxas: false);
        editado.NomeCliente = "Novo Cliente";

        aluguel.AtualizarRegistro(editado);

        Assert.Equal(id, aluguel.Id);
        Assert.Equal(TipoPlano.Livre, aluguel.TipoPlano);
        Assert.Equal(5, aluguel.Dias);
        Assert.Equal("Novo Cliente", aluguel.NomeCliente);
        Assert.Empty(aluguel.Taxas);
    }

    [Fact]
    public void AtualizarRegistro_cria_copias_novas_das_taxas()
    {
        var aluguel = Criar(comTaxas: false);
        var editado = Criar();

        aluguel.AtualizarRegistro(editado);

        Assert.Equal(2, aluguel.Taxas.Count);
        Assert.DoesNotContain(aluguel.Taxas, t => editado.Taxas.Any(o => o.Id == t.Id));
    }

    [Theory]
    [InlineData(TipoCobrancaTaxa.Fixo, 50, 7, 50)]
    [InlineData(TipoCobrancaTaxa.Diario, 20, 7, 140)]
    public void Taxa_fixa_cobra_uma_vez_e_diaria_multiplica_pelos_dias(
        TipoCobrancaTaxa tipo, decimal preco, int dias, decimal esperado)
    {
        var taxa = new AluguelTaxa(Guid.NewGuid(), "Item", preco, tipo);

        Assert.Equal(esperado, taxa.ValorPara(dias));
    }

    // ---------------- cupom de desconto ----------------

    [Fact]
    public void Cupom_abate_valor_fixo_do_previsto_sem_tocar_na_caucao()
    {
        var aluguel = Criar();
        aluguel.CupomValor = 100m;

        var previsto = aluguel.CalcularPrevisto();

        Assert.Equal(100m, previsto.ValorDesconto);
        Assert.Equal(1550m, previsto.ValorTotalPrevisto); // 1650 - 100
    }

    [Fact]
    public void Cupom_maior_que_o_consumo_e_limitado_e_a_caucao_continua_inteira()
    {
        var aluguel = Criar(TipoPlano.Livre, dias: 1, seguro: 0m, comTaxas: false);
        aluguel.CupomValor = 5000m;

        var previsto = aluguel.CalcularPrevisto();

        Assert.Equal(300m, previsto.ValorDesconto);
        Assert.Equal(1000m, previsto.ValorTotalPrevisto);
    }

    [Fact]
    public void Cupom_tambem_abate_o_valor_final_mas_nao_a_multa_nem_o_combustivel()
    {
        var aluguel = Criar(TipoPlano.Controlado, dias: 2, seguro: 0m, comTaxas: false);
        aluguel.CupomValor = 50m;

        // Sem cupom: 1000 + 650 + 330 + 65 = 2045. Com cupom de 50: 1995.
        var final = aluguel.CalcularFinal(Saida.AddDays(3), 300 + 1000, tanqueCheio: false, 5.5m, 60);

        Assert.Equal(50m, final.ValorDesconto);
        Assert.Equal(65m, final.ValorMulta);
        Assert.Equal(330m, final.ValorCombustivel);
        Assert.Equal(1995m, final.ValorFinal);
    }

    [Fact]
    public void Aluguel_sem_cupom_nao_tem_desconto()
    {
        var aluguel = Criar();

        Assert.Equal(0m, aluguel.CalcularPrevisto().ValorDesconto);
        Assert.Equal(0m, aluguel.CalcularFinal(Saida.AddDays(3), 1000, true, 0m, 0).ValorDesconto);
    }

    [Fact]
    public void AtualizarRegistro_copia_o_cupom()
    {
        var aluguel = Criar();
        var editado = Criar();
        var cupom = Guid.NewGuid();
        editado.CupomId = cupom;
        editado.CupomNome = "PROMO10";
        editado.CupomValor = 10m;

        aluguel.AtualizarRegistro(editado);

        Assert.Equal(cupom, aluguel.CupomId);
        Assert.Equal("PROMO10", aluguel.CupomNome);
        Assert.Equal(10m, aluguel.CupomValor);
    }
}
