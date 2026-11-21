using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class CupomIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private static object Cupom(Guid parceiro, string nome = "PROMO100", decimal valor = 100m, int validadeEmDias = 30) =>
        new { nome, valor, dataValidade = Dia(validadeEmDias), parceiroId = parceiro };

    private static object Aluguel(Cenario c, Guid carro, Guid? cupom, int saida = 0, int retorno = 3, Guid? condutor = null) => new
    {
        condutorId = condutor ?? c.CondutorId,
        automovelId = carro,
        tipoPlano = "Diario",
        dataSaida = Dia(saida),
        dataRetornoPrevista = Dia(retorno),
        kmInicial = 1000,
        tipoSeguro = "Nenhum",
        taxasIds = Array.Empty<Guid>(),
        cupomId = cupom
    };

    // ---------------- parceiros ----------------

    [Fact]
    public async Task Parceiro_crud_nome_unico_e_so_empresa_escreve()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        var criado = await c.Api.Post("/api/parceiros", new { nome = "Hotel Central" });
        Assert.Equal(201, criado.Codigo);

        Assert.Equal(409, (await c.Api.Post("/api/parceiros", new { nome = "Hotel Central" })).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/parceiros", new { nome = "" })).Codigo);

        Assert.Equal(200, (await c.Api.Put($"/api/parceiros/{criado.Id}", new { nome = "Hotel Central Plaza" })).Codigo);
        Assert.Equal("Hotel Central Plaza", (await c.Api.Get($"/api/parceiros/{criado.Id}")).Texto("nome"));

        var outro = await c.Api.Post("/api/parceiros", new { nome = "Agencia Sol" });
        Assert.Equal(409, (await c.Api.Put($"/api/parceiros/{outro.Id}", new { nome = "Hotel Central Plaza" })).Codigo);

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(2, (await c.Api.Get("/api/parceiros")).Registros.GetArrayLength());
        Assert.Equal(403, (await c.Api.Post("/api/parceiros", new { nome = "Nao Pode" })).Codigo);
        Assert.Equal(403, (await c.Api.Delete($"/api/parceiros/{criado.Id}")).Codigo);

        c.Api.UsarToken(c.TokenEmpresa);
        Assert.Equal(204, (await c.Api.Delete($"/api/parceiros/{outro.Id}")).Codigo);
        Assert.Equal(404, (await c.Api.Get($"/api/parceiros/{outro.Id}")).Codigo);
    }

    [Fact]
    public async Task Parceiro_vinculado_a_cupom_nao_pode_ser_excluido()
    {
        var c = await Criar(fabrica);
        var parceiro = await c.Api.Post("/api/parceiros", new { nome = "Parceiro Com Cupom" });
        var cupom = await c.Api.Post("/api/cupons", Cupom(parceiro.Id));

        Assert.Equal(422, (await c.Api.Delete($"/api/parceiros/{parceiro.Id}")).Codigo);

        Assert.Equal(204, (await c.Api.Delete($"/api/cupons/{cupom.Id}")).Codigo);
        Assert.Equal(204, (await c.Api.Delete($"/api/parceiros/{parceiro.Id}")).Codigo);
    }

    [Fact]
    public async Task Parceiros_de_uma_empresa_nao_aparecem_para_outra()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var parceiro = await a.Api.Post("/api/parceiros", new { nome = "So da A" });

        Assert.Equal(404, (await b.Api.Get($"/api/parceiros/{parceiro.Id}")).Codigo);
        Assert.Equal(404, (await b.Api.Delete($"/api/parceiros/{parceiro.Id}")).Codigo);
        Assert.Equal(404, (await b.Api.Post("/api/cupons", Cupom(parceiro.Id))).Codigo);
    }

    // ---------------- cupons ----------------

    [Fact]
    public async Task Cupom_crud_nome_unico_validacoes_e_ativo_conforme_a_validade()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;

        var criado = await c.Api.Post("/api/cupons", Cupom(parceiro, "VERAO50", 50m));
        Assert.Equal(201, criado.Codigo);

        var lido = await c.Api.Get($"/api/cupons/{criado.Id}");
        Assert.Equal(50m, lido.Numero("valor"));
        Assert.Equal("Parceiro", lido.Texto("nomeParceiro"));
        Assert.True(lido.Corpo.GetProperty("ativo").GetBoolean());

        Assert.Equal(409, (await c.Api.Post("/api/cupons", Cupom(parceiro, "VERAO50"))).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/cupons", Cupom(parceiro, "ZERO", valor: 0m))).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/cupons", Cupom(parceiro, "PASSADO", validadeEmDias: -1))).Codigo);
        Assert.Equal(404, (await c.Api.Post("/api/cupons", Cupom(Guid.NewGuid(), "SEMPAI"))).Codigo);

        // Editar para uma data passada encerra o cupom.
        var encerrado = await c.Api.Put($"/api/cupons/{criado.Id}", Cupom(parceiro, "VERAO50", 60m, validadeEmDias: -1));
        Assert.Equal(200, encerrado.Codigo);
        Assert.False(encerrado.Corpo.GetProperty("ativo").GetBoolean());
        Assert.Equal(60m, encerrado.Numero("valor"));

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(1, (await c.Api.Get("/api/cupons")).Registros.GetArrayLength());
        Assert.Equal(403, (await c.Api.Post("/api/cupons", Cupom(parceiro, "NAOPODE"))).Codigo);
        Assert.Equal(403, (await c.Api.Get("/api/cupons/mais-utilizados")).Codigo);
    }

    // ---------------- cupom no aluguel ----------------

    [Fact]
    public async Task Cupom_abate_o_valor_do_aluguel_e_fica_guardado_no_aluguel()
    {
        var c = await Criar(fabrica);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;
        var cupom = (await c.Api.Post("/api/cupons", Cupom(parceiro, "PROMO100", 100m))).Id;
        var carro = await c.NovoAutomovel();

        // 3 diárias de R$ 150 = 450; com cupom de 100 -> 1000 de caução + 350.
        var simulacao = await c.Api.Post("/api/alugueis/calcular", Aluguel(c, carro, cupom));
        Assert.Equal(200, simulacao.Codigo);
        Assert.Equal(100m, simulacao.Corpo.GetProperty("previsto").GetProperty("valorDesconto").GetDecimal());
        Assert.Equal(1350m, simulacao.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());

        var aluguel = await c.Api.Post("/api/alugueis", Aluguel(c, carro, cupom));
        Assert.Equal(201, aluguel.Codigo);

        var lido = await c.Api.Get($"/api/alugueis/{aluguel.Id}");
        Assert.Equal("PROMO100", lido.Texto("nomeCupom"));
        Assert.Equal(1350m, lido.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());

        var devolvido = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao",
            new { dataDevolucao = Dia(3), kmFinal = 1200, tanqueCheio = true });

        var final = devolvido.Corpo.GetProperty("devolucao").GetProperty("final");
        Assert.Equal(100m, final.GetProperty("valorDesconto").GetDecimal());
        Assert.Equal(1000m + 450m + 300m - 100m, final.GetProperty("valorFinal").GetDecimal());
    }

    [Fact]
    public async Task Cupom_percentual_desconta_a_porcentagem_do_consumo_e_guarda_o_tipo_no_aluguel()
    {
        var c = await Criar(fabrica);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;

        // A porcentagem não passa de 100; sem informar o tipo, continua sendo valor fixo.
        Assert.Equal(400, (await c.Api.Post("/api/cupons", new
        {
            nome = "MUITO", valor = 150m, dataValidade = Dia(30), parceiroId = parceiro, tipo = "Percentual"
        })).Codigo);

        var criado = await c.Api.Post("/api/cupons", new
        {
            nome = "DEZPORCENTO", valor = 10m, dataValidade = Dia(30), parceiroId = parceiro, tipo = "Percentual"
        });
        Assert.Equal(201, criado.Codigo);

        var lido = await c.Api.Get($"/api/cupons/{criado.Id}");
        Assert.Equal("Percentual", lido.Texto("tipo"));
        Assert.Equal(10m, lido.Numero("valor"));

        var fixo = await c.Api.Get($"/api/cupons/{(await c.Api.Post("/api/cupons", Cupom(parceiro, "FIXO20", 20m))).Id}");
        Assert.Equal("Valor", fixo.Texto("tipo"));

        var carro = await c.NovoAutomovel();

        // 3 diárias de R$ 150 = 450; 10% = 45 -> 1000 de caução + 405.
        var simulacao = await c.Api.Post("/api/alugueis/calcular", Aluguel(c, carro, criado.Id));
        Assert.Equal(45m, simulacao.Corpo.GetProperty("previsto").GetProperty("valorDesconto").GetDecimal());
        Assert.Equal(1405m, simulacao.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());

        var aluguel = await c.Api.Post("/api/alugueis", Aluguel(c, carro, criado.Id));
        Assert.Equal(201, aluguel.Codigo);

        var gravado = await c.Api.Get($"/api/alugueis/{aluguel.Id}");
        Assert.Equal("Percentual", gravado.Texto("cupomTipo"));
        Assert.Equal(10m, gravado.Numero("cupomValor"));

        // Editar o cupom depois não muda o aluguel já feito (ele guarda a própria cópia).
        await c.Api.Put($"/api/cupons/{criado.Id}", new
        {
            nome = "DEZPORCENTO", valor = 50m, dataValidade = Dia(30), parceiroId = parceiro, tipo = "Percentual"
        });
        var depois = await c.Api.Get($"/api/alugueis/{aluguel.Id}");
        Assert.Equal(1405m, depois.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());
    }

    [Fact]
    public async Task Cupom_expirado_ou_inexistente_nao_pode_ser_usado()
    {
        var c = await Criar(fabrica);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;
        var cupom = (await c.Api.Post("/api/cupons", Cupom(parceiro, "VELHO", 50m))).Id;
        var carro = await c.NovoAutomovel();

        await c.Api.Put($"/api/cupons/{cupom}", Cupom(parceiro, "VELHO", 50m, validadeEmDias: -2));

        var expirado = await c.Api.Post("/api/alugueis", Aluguel(c, carro, cupom));

        Assert.Equal(400, expirado.Codigo);
        Assert.Contains("expirou", expirado.Bruto);
        Assert.Equal(400, (await c.Api.Post("/api/alugueis/calcular", Aluguel(c, carro, cupom))).Codigo);
        Assert.Equal(404, (await c.Api.Post("/api/alugueis", Aluguel(c, carro, Guid.NewGuid()))).Codigo);
    }

    [Fact]
    public async Task Cliente_so_pode_usar_o_mesmo_cupom_uma_vez_mas_outro_cliente_pode()
    {
        var c = await Criar(fabrica);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;
        var cupom = (await c.Api.Post("/api/cupons", Cupom(parceiro, "UMAVEZ", 80m))).Id;
        var carro1 = await c.NovoAutomovel();
        var carro2 = await c.NovoAutomovel();

        var primeiro = await c.Api.Post("/api/alugueis", Aluguel(c, carro1, cupom));
        Assert.Equal(201, primeiro.Codigo);

        // Mesmo cliente (PJ), outro condutor dele, outro carro: o cupom já foi usado por este cliente.
        var repetido = await c.Api.Post("/api/alugueis", Aluguel(c, carro2, cupom, condutor: c.CondutorCnhCurtaId, retorno: 4));
        Assert.Equal(409, repetido.Codigo);

        // Outro cliente pode usar.
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", Aluguel(c, carro2, cupom, condutor: c.CondutorPfId))).Codigo);

        // Editar o próprio aluguel mantendo o cupom não conta como segundo uso.
        Assert.Equal(200, (await c.Api.Put($"/api/alugueis/{primeiro.Id}", Aluguel(c, carro1, cupom, retorno: 2))).Codigo);

        // Excluir o aluguel (depois de devolvido) libera o cupom de novo para o cliente.
        await c.Api.Post($"/api/alugueis/{primeiro.Id}/devolucao", new { dataDevolucao = Dia(2), kmFinal = 1000, tanqueCheio = true });
        Assert.Equal(204, (await c.Api.Delete($"/api/alugueis/{primeiro.Id}")).Codigo);
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", Aluguel(c, carro1, cupom, saida: 10, retorno: 12))).Codigo);
    }

    [Fact]
    public async Task Aluguel_que_ja_usa_o_cupom_pode_ser_editado_mesmo_depois_de_o_cupom_vencer()
    {
        var c = await Criar(fabrica);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;
        var cupom = (await c.Api.Post("/api/cupons", Cupom(parceiro, "EXPIRA", 40m))).Id;
        var carro = await c.NovoAutomovel();
        var aluguel = await c.Api.Post("/api/alugueis", Aluguel(c, carro, cupom));

        await c.Api.Put($"/api/cupons/{cupom}", Cupom(parceiro, "EXPIRA", 40m, validadeEmDias: -1));

        var editado = await c.Api.Put($"/api/alugueis/{aluguel.Id}", Aluguel(c, carro, cupom, retorno: 4));

        Assert.Equal(200, editado.Codigo);
        Assert.Equal("EXPIRA", editado.Texto("nomeCupom"));
    }

    [Fact]
    public async Task Cupom_em_aluguel_aberto_nao_pode_ser_excluido_e_aluguel_concluido_guarda_o_valor()
    {
        var c = await Criar(fabrica);
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro" })).Id;
        var cupom = (await c.Api.Post("/api/cupons", Cupom(parceiro, "BLOQUEIA", 70m))).Id;
        var carro = await c.NovoAutomovel();
        var aluguel = await c.Api.Post("/api/alugueis", Aluguel(c, carro, cupom));

        Assert.Equal(422, (await c.Api.Delete($"/api/cupons/{cupom}")).Codigo);

        var devolvido = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao",
            new { dataDevolucao = Dia(3), kmFinal = 1000, tanqueCheio = true });
        var totalAntes = devolvido.Corpo.GetProperty("devolucao").GetProperty("final").GetProperty("valorFinal").GetDecimal();

        // Depois de concluído, o cupom pode ser excluído e o aluguel segue com o mesmo valor.
        Assert.Equal(204, (await c.Api.Delete($"/api/cupons/{cupom}")).Codigo);

        var lido = await c.Api.Get($"/api/alugueis/{aluguel.Id}");
        Assert.Equal(totalAntes, lido.Corpo.GetProperty("devolucao").GetProperty("final").GetProperty("valorFinal").GetDecimal());
    }

    // ---------------- ranking ----------------

    [Fact]
    public async Task Ranking_mostra_o_cupom_e_o_parceiro_mais_utilizados()
    {
        var c = await Criar(fabrica);
        var parceiroA = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro A" })).Id;
        var parceiroB = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro B" })).Id;
        var cupomA = (await c.Api.Post("/api/cupons", Cupom(parceiroA, "CUPOM-A", 50m))).Id;
        var cupomB = (await c.Api.Post("/api/cupons", Cupom(parceiroB, "CUPOM-B", 30m))).Id;

        Assert.Empty((await c.Api.Get("/api/cupons/mais-utilizados")).Corpo.GetProperty("cupons").EnumerateArray());

        // Dois clientes usam o cupom A, um usa o B.
        var carros = new[] { await c.NovoAutomovel(), await c.NovoAutomovel(), await c.NovoAutomovel() };

        Assert.Equal(201, (await c.Api.Post("/api/alugueis", Aluguel(c, carros[0], cupomA))).Codigo);
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", Aluguel(c, carros[1], cupomA, condutor: c.CondutorPfId))).Codigo);
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", Aluguel(c, carros[2], cupomB))).Codigo);

        var ranking = await c.Api.Get("/api/cupons/mais-utilizados");
        Assert.Equal(200, ranking.Codigo);

        var cupons = ranking.Corpo.GetProperty("cupons");
        Assert.Equal(2, cupons.GetArrayLength());
        Assert.Equal("CUPOM-A", cupons[0].GetProperty("nome").GetString());
        Assert.Equal(2, cupons[0].GetProperty("usos").GetInt32());
        Assert.Equal("Parceiro A", cupons[0].GetProperty("nomeParceiro").GetString());
        Assert.Equal("CUPOM-B", cupons[1].GetProperty("nome").GetString());
        Assert.Equal(1, cupons[1].GetProperty("usos").GetInt32());

        var parceiros = ranking.Corpo.GetProperty("parceiros");
        Assert.Equal("Parceiro A", parceiros[0].GetProperty("nomeParceiro").GetString());
        Assert.Equal(2, parceiros[0].GetProperty("usos").GetInt32());
    }
}
