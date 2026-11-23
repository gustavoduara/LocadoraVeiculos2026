using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

/// <summary>Taxas e serviços e cupom no pedido de reserva: o cliente vê o mesmo valor que a locadora aprova.</summary>
public class ReservaTaxasCupomIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private static object Dados(string cpf) => new
    {
        cpf, rg = "1234567", cnh = "12345678901", validadeCnh = Dia(1000), telefone = "(47) 99999-1234",
        estado = "SC", cidade = "Rio do Sul", bairro = "Centro", rua = "Rua XV", numero = "10"
    };

    /// <summary>3 dias, plano Diário (R$ 150/dia), seguro do cliente (R$ 30/dia): 450 + 90 de consumo, mais a caução de 1000.</summary>
    private static object Pedido(Guid carro, Guid[]? taxas = null, string? cupom = null, int retirada = 1, int devolucao = 4, string seguro = "Contratado") => new
    {
        automovelId = carro,
        retirada = Dia(retirada),
        devolucao = Dia(devolucao),
        tipoPlano = "Diario",
        tipoSeguro = seguro,
        taxasIds = taxas ?? [],
        cupom
    };

    private async Task<ClienteApi> NovoCliente()
    {
        var api = new ClienteApi(fabrica.CreateClient());

        api.UsarToken(await CriarContaEEntrar(api, "/api/auth/registrar-cliente", "Beatriz Lima", Geradores.Email("taxas")));
        Assert.Equal(200, (await api.Put("/api/meus-dados", Dados(Geradores.Cpf()))).Codigo);

        return api;
    }

    private static async Task<Guid> NovoCupom(Cenario c, string nome, decimal valor, string tipo = "Percentual")
    {
        var parceiro = (await c.Api.Post("/api/parceiros", new { nome = "Parceiro " + nome })).Id;
        var r = await c.Api.Post("/api/cupons", new { nome, valor, tipo, dataValidade = Dia(30), parceiroId = parceiro });
        Assert.Equal(201, r.Codigo);

        return r.Id;
    }

    private static decimal Numero(Resposta r, string propriedade) => r.Corpo.GetProperty(propriedade).GetDecimal();

    // ---------------- o que a locadora oferece ----------------

    [Fact]
    public async Task Detalhe_do_carro_lista_as_taxas_da_locadora_e_so_dela()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carro = await a.NovoAutomovel();
        await b.NovoAutomovel();
        var cliente = await NovoCliente();

        var detalhe = await cliente.Get($"/api/catalogo/{carro}");
        Assert.Equal(200, detalhe.Codigo);

        var taxas = detalhe.Corpo.GetProperty("taxas").EnumerateArray().ToList();

        Assert.Equal(2, taxas.Count);
        Assert.Contains(taxas, t => t.GetProperty("nome").GetString() == "GPS" && t.GetProperty("preco").GetDecimal() == 20m && t.GetProperty("tipoCobranca").GetString() == "Diario");
        Assert.Contains(taxas, t => t.GetProperty("nome").GetString() == "Cadeirinha" && t.GetProperty("tipoCobranca").GetString() == "Fixo");
    }

    // ---------------- estimativa ----------------

    [Fact]
    public async Task Estimativa_soma_as_taxas_com_cobranca_fixa_e_diaria()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var cliente = await NovoCliente();

        var sem = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro));
        Assert.Equal(200, sem.Codigo);
        Assert.Equal(0m, Numero(sem, "valorTaxas"));
        Assert.Equal(1540m, Numero(sem, "valorTotalPrevisto")); // 450 + 90 + 1000

        // GPS: 20/dia x 3 = 60; cadeirinha: 50 uma vez.
        var com = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, [c.TaxaDiariaId, c.TaxaFixaId]));
        Assert.Equal(200, com.Codigo);
        Assert.Equal(110m, Numero(com, "valorTaxas"));
        Assert.Equal(1650m, Numero(com, "valorTotalPrevisto"));

        var linhas = com.Corpo.GetProperty("taxas").EnumerateArray().ToList();
        Assert.Equal(60m, linhas.Single(l => l.GetProperty("nome").GetString() == "GPS").GetProperty("valor").GetDecimal());
        Assert.Equal(50m, linhas.Single(l => l.GetProperty("nome").GetString() == "Cadeirinha").GetProperty("valor").GetDecimal());
    }

    [Fact]
    public async Task Taxa_de_outra_locadora_ou_que_nao_existe_e_recusada()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carro = await a.NovoAutomovel();
        var cliente = await NovoCliente();

        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, [b.TaxaDiariaId]))).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, [Guid.NewGuid()]))).Codigo);

        // Repetir a mesma taxa não cobra duas vezes.
        var repetida = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, [a.TaxaFixaId, a.TaxaFixaId]));
        Assert.Equal(50m, Numero(repetida, "valorTaxas"));
    }

    [Fact]
    public async Task Cupom_percentual_e_de_valor_fixo_abatem_so_do_consumo_e_o_nome_nao_diferencia_maiusculas()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        await NovoCupom(c, "DEZPORCENTO", 10m);
        await NovoCupom(c, "VINTEREAIS", 20m, "Valor");
        var cliente = await NovoCliente();

        // Consumo de 650 (450 + 90 + 110 de taxas): 10% = 65.
        var percentual = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, [c.TaxaDiariaId, c.TaxaFixaId], cupom: "dezporcento"));
        Assert.Equal(200, percentual.Codigo);
        Assert.Equal(650m, 450m + 90m + Numero(percentual, "valorTaxas") - 0m);
        Assert.Equal(65m, Numero(percentual, "valorDesconto"));
        Assert.Equal(1585m, Numero(percentual, "valorTotalPrevisto"));
        Assert.Equal("DEZPORCENTO", percentual.Texto("cupomNome"));
        Assert.Equal("Percentual", percentual.Texto("cupomTipo"));

        var fixo = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, cupom: " VINTEREAIS "));
        Assert.Equal(20m, Numero(fixo, "valorDesconto"));
        Assert.Equal(1520m, Numero(fixo, "valorTotalPrevisto")); // 540 - 20 + 1000
    }

    [Fact]
    public async Task Cupom_inexistente_vencido_ou_de_outra_locadora_e_recusado_com_mensagem()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carro = await a.NovoAutomovel();
        var cupomDaA = await NovoCupom(a, "SOPARAA", 10m);
        await NovoCupom(b, "SOPARAB", 10m);
        var cliente = await NovoCliente();

        var inexistente = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, cupom: "NAOEXISTE"));
        Assert.Equal(400, inexistente.Codigo);
        Assert.Contains("Não encontramos este cupom", inexistente.Bruto);

        // O cupom de outra locadora não vale neste carro.
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, cupom: "SOPARAB"))).Codigo);

        // Vencido: a locadora encerra o cupom (data no passado) e ele deixa de valer.
        var parceiro = (await a.Api.Get($"/api/cupons/{cupomDaA}")).Texto("parceiroId");
        Assert.Equal(200, (await a.Api.Put($"/api/cupons/{cupomDaA}",
            new { nome = "SOPARAA", valor = 10m, tipo = "Percentual", dataValidade = Dia(-1), parceiroId = parceiro })).Codigo);

        var vencido = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, cupom: "SOPARAA"));
        Assert.Equal(400, vencido.Codigo);
        Assert.Contains("expirou", vencido.Bruto);
    }

    // ---------------- pedido e aprovação ----------------

    [Fact]
    public async Task Pedido_guarda_taxas_e_cupom_e_a_aprovacao_cria_um_aluguel_com_o_mesmo_valor()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        await NovoCupom(c, "DEZPORCENTO", 10m);
        var cliente = await NovoCliente();

        var pedido = Pedido(carro, [c.TaxaDiariaId, c.TaxaFixaId], "DEZPORCENTO");

        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", pedido)).Codigo);

        // O cliente vê o que pediu...
        var minha = (await cliente.Get("/api/minhas-reservas")).Registros.EnumerateArray().Single();
        Assert.Equal(110m, minha.GetProperty("valorTaxas").GetDecimal());
        Assert.Equal(65m, minha.GetProperty("valorDesconto").GetDecimal());
        Assert.Equal(1585m, minha.GetProperty("valorTotalPrevisto").GetDecimal());
        Assert.Equal("DEZPORCENTO", minha.GetProperty("cupomNome").GetString());
        Assert.Equal(2, minha.GetProperty("taxas").GetArrayLength());

        // ... e a locadora vê o mesmo.
        var recebida = (await c.Api.Get("/api/reservas?status=Pendente")).Registros.EnumerateArray().Single();
        Assert.Equal(1585m, recebida.GetProperty("valorTotalPrevisto").GetDecimal());
        Assert.Equal(2, recebida.GetProperty("taxas").GetArrayLength());

        var id = recebida.GetProperty("id").GetGuid();
        Assert.Equal(200, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 1000 })).Codigo);

        // O aluguel nasce com as mesmas taxas e o mesmo cupom, e o total é o que o cliente viu.
        var aluguelId = (await c.Api.Get("/api/reservas?status=Aprovada")).Registros.EnumerateArray().Single().GetProperty("aluguelId").GetGuid();
        var aluguel = await c.Api.Get($"/api/alugueis/{aluguelId}");

        Assert.Equal(1585m, aluguel.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());
        Assert.Equal(65m, aluguel.Corpo.GetProperty("previsto").GetProperty("valorDesconto").GetDecimal());
        Assert.Equal(2, aluguel.Corpo.GetProperty("taxas").GetArrayLength());
        Assert.Equal("DEZPORCENTO", aluguel.Texto("nomeCupom"));
    }

    [Fact]
    public async Task Cada_cliente_usa_o_cupom_uma_vez_por_locadora_mas_outro_cliente_pode()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var outroCarro = await c.NovoAutomovel();
        await NovoCupom(c, "UMAVEZ", 10m);
        var primeiro = await NovoCliente();
        var segundo = await NovoCliente();

        Assert.Equal(201, (await primeiro.Post("/api/minhas-reservas", Pedido(carro, cupom: "UMAVEZ"))).Codigo);

        // O mesmo cliente não repete o cupom (nem na estimativa, nem em outro pedido).
        var repetido = await primeiro.Post("/api/minhas-reservas/calcular", Pedido(outroCarro, cupom: "UMAVEZ", retirada: 10, devolucao: 12));
        Assert.Equal(400, repetido.Codigo);
        Assert.Contains("já usou", repetido.Bruto);
        Assert.Equal(400, (await primeiro.Post("/api/minhas-reservas", Pedido(outroCarro, cupom: "UMAVEZ", retirada: 10, devolucao: 12))).Codigo);

        // Outro cliente usa normalmente, e o primeiro continua podendo pedir sem cupom.
        Assert.Equal(200, (await segundo.Post("/api/minhas-reservas/calcular", Pedido(carro, cupom: "UMAVEZ", retirada: 10, devolucao: 12))).Codigo);
        Assert.Equal(201, (await primeiro.Post("/api/minhas-reservas", Pedido(outroCarro, retirada: 10, devolucao: 12))).Codigo);

        // Cancelar o pedido libera o cupom de novo.
        var pedidoComCupom = (await primeiro.Get("/api/minhas-reservas")).Registros.EnumerateArray()
            .First(r => r.GetProperty("cupomNome").GetString() == "UMAVEZ").GetProperty("id").GetGuid();
        Assert.Equal(200, (await primeiro.Post($"/api/minhas-reservas/{pedidoComCupom}/cancelar")).Codigo);
        Assert.Equal(200, (await primeiro.Post("/api/minhas-reservas/calcular", Pedido(carro, cupom: "UMAVEZ", retirada: 20, devolucao: 22))).Codigo);
    }

    [Fact]
    public async Task Aprovar_falha_com_mensagem_se_o_cupom_venceu_ou_a_taxa_saiu_do_catalogo_e_o_pedido_continua_pendente()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var cupom = await NovoCupom(c, "VENCE", 10m);
        var cliente = await NovoCliente();

        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro, [c.TaxaFixaId], "VENCE"))).Codigo);
        var id = (await c.Api.Get("/api/reservas")).Registros[0].GetProperty("id").GetGuid();

        // A locadora encerra o cupom antes de responder: a aprovação diz o motivo.
        var parceiro = (await c.Api.Get($"/api/cupons/{cupom}")).Texto("parceiroId");
        await c.Api.Put($"/api/cupons/{cupom}", new { nome = "VENCE", valor = 10m, tipo = "Percentual", dataValidade = Dia(-1), parceiroId = parceiro });

        var comCupomVencido = await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 1000 });
        Assert.Equal(400, comCupomVencido.Codigo);
        Assert.Contains("expirou", comCupomVencido.Bruto);

        // Depois, a taxa também sai do catálogo.
        Assert.Equal(204, (await c.Api.Delete($"/api/taxas-servicos/{c.TaxaFixaId}")).Codigo);
        Assert.NotEqual(200, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 1000 })).Codigo);

        Assert.Equal(1, (await c.Api.Get("/api/reservas?status=Pendente")).Registros.GetArrayLength());
    }
}
