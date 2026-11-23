using Microsoft.Extensions.DependencyInjection;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class ReservaIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private EmailsDeTeste Emails => fabrica.Services.GetRequiredService<EmailsDeTeste>();

    /// <summary>Uma pessoa com conta de cliente. Com <paramref name="comDados"/> ela já preencheu Meus dados.</summary>
    private async Task<(ClienteApi Api, string Email, string Cpf)> NovoCliente(bool comDados = true, int validadeCnhEmDias = 1000)
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var email = Geradores.Email("pessoa");

        api.UsarToken(await Cenario.CriarContaEEntrar(api, "/api/auth/registrar-cliente", "Beatriz Lima", email));

        var cpf = Geradores.Cpf();

        if (comDados)
            Assert.Equal(200, (await api.Put("/api/meus-dados", Dados(cpf, validadeCnhEmDias))).Codigo);

        return (api, email, cpf);
    }

    private static object Dados(string cpf, int validadeCnhEmDias = 1000) => new
    {
        cpf,
        rg = "1234567",
        cnh = "12345678901",
        validadeCnh = Dia(validadeCnhEmDias),
        telefone = "(47) 99999-1234",
        estado = "SC",
        cidade = "Rio do Sul",
        bairro = "Centro",
        rua = "Rua XV",
        numero = "10"
    };

    private static object Pedido(Guid carro, int retirada = 1, int devolucao = 4, string plano = "Diario", string seguro = "Nenhum") => new
    {
        automovelId = carro,
        retirada = Dia(retirada),
        devolucao = Dia(devolucao),
        tipoPlano = plano,
        tipoSeguro = seguro
    };

    // ---------------- dados da locadora ----------------

    [Fact]
    public async Task Locadora_define_nome_fantasia_e_telefone_e_so_a_empresa_altera()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        var vazio = await c.Api.Get("/api/configuracoes/locadora");
        Assert.Equal(200, vazio.Codigo);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, vazio.Corpo.GetProperty("nomeFantasia").ValueKind);

        var salvo = await c.Api.Put("/api/configuracoes/locadora", new { nomeFantasia = "Serra Aluguel de Carros", telefone = "(49) 99113-0685" });
        Assert.Equal(200, salvo.Codigo);
        Assert.Equal("Serra Aluguel de Carros", (await c.Api.Get("/api/configuracoes/locadora")).Texto("nomeFantasia"));

        Assert.Equal(400, (await c.Api.Put("/api/configuracoes/locadora", new { nomeFantasia = "Serra", telefone = "12345" })).Codigo);
        Assert.Equal(400, (await c.Api.Put("/api/configuracoes/locadora", new { nomeFantasia = "S", telefone = (string?)null })).Codigo);

        // Os preços continuam intactos e funcionário só lê.
        Assert.Equal(6m, (await c.Api.Get("/api/configuracoes")).Numero("precoGasolina"));

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(200, (await c.Api.Get("/api/configuracoes/locadora")).Codigo);
        Assert.Equal(403, (await c.Api.Put("/api/configuracoes/locadora", new { nomeFantasia = "Outro", telefone = (string?)null })).Codigo);
    }

    [Fact]
    public async Task Nome_fantasia_aparece_no_catalogo_no_lugar_do_nome_da_conta()
    {
        var c = await Criar(fabrica);
        string nomeUnico = "Locadora " + Guid.NewGuid().ToString("N")[..8];
        await c.Api.Put("/api/configuracoes/locadora", new { nomeFantasia = nomeUnico, telefone = (string?)null });

        string marca = "Yy" + Guid.NewGuid().ToString("N")[..8];
        await c.Api.Post("/api/automoveis", new
        {
            placa = Geradores.Placa(), marca, modelo = "Modelo", cor = "Azul", ano = 2024, tipoCombustivel = "Gasolina",
            capacidadeCombustivelLitros = 50, grupoVeiculosId = c.GrupoId
        });

        var (cliente, _, _) = await NovoCliente(comDados: false);
        var r = await cliente.Get($"/api/catalogo?texto={marca}");

        Assert.Equal(nomeUnico, r.Corpo.GetProperty("registros")[0].GetProperty("locadora").GetString());
    }

    // ---------------- meus dados ----------------

    [Fact]
    public async Task Meus_dados_comecam_vazios_validam_documentos_e_so_o_cliente_acessa()
    {
        var (api, _, cpf) = await NovoCliente(comDados: false);

        var vazio = await api.Get("/api/meus-dados");
        Assert.Equal(200, vazio.Codigo);
        Assert.False(vazio.Corpo.GetProperty("completo").GetBoolean());

        Assert.Equal(400, (await api.Put("/api/meus-dados", Dados("111.111.111-11"))).Codigo);

        var salvo = await api.Put("/api/meus-dados", Dados(cpf));
        Assert.Equal(200, salvo.Codigo);
        Assert.True(salvo.Corpo.GetProperty("completo").GetBoolean());
        Assert.False(salvo.Corpo.GetProperty("cnhVencida").GetBoolean());
        Assert.Equal(cpf, (await api.Get("/api/meus-dados")).Texto("cpf"));

        // CNH vencida pode ser salva (vem marcada), como no cadastro de condutor.
        Assert.True((await api.Put("/api/meus-dados", Dados(cpf, -10))).Corpo.GetProperty("cnhVencida").GetBoolean());

        var locadora = await Criar(fabrica);
        Assert.Equal(403, (await locadora.Api.Get("/api/meus-dados")).Codigo);
    }

    [Fact]
    public async Task Cada_carro_tem_o_proprio_seguro_e_sem_valor_so_se_reserva_sem_seguro()
    {
        var c = await Criar(fabrica);
        var padrao = await c.NovoAutomovel();
        var caro = await c.NovoAutomovel(seguroDiaria: 55.5m);
        var semSeguro = await c.NovoAutomovel(seguroDiaria: 0m);
        var (cliente, _, _) = await NovoCliente(comDados: false);

        Assert.Equal(30m, (await cliente.Get($"/api/catalogo/{padrao}")).Corpo.GetProperty("seguroDiaria").GetDecimal());
        Assert.Equal(55.5m, (await cliente.Get($"/api/catalogo/{caro}")).Corpo.GetProperty("seguroDiaria").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await cliente.Get($"/api/catalogo/{semSeguro}")).Corpo.GetProperty("seguroDiaria").ValueKind);

        // 3 dias × R$ 55,50 (o valor do carro, não de uma tabela da locadora).
        var comSeguro = await cliente.Post("/api/minhas-reservas/calcular", Pedido(caro, seguro: "Contratado"));
        Assert.Equal(200, comSeguro.Codigo);
        Assert.Equal(166.5m, comSeguro.Numero("valorSeguro"));

        // O mesmo pedido sem seguro não cobra nada a mais.
        Assert.Equal(0m, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(caro, seguro: "Nenhum"))).Numero("valorSeguro"));

        // Carro sem valor de seguro: com seguro é recusado, sem seguro segue normal.
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(semSeguro, seguro: "Contratado"))).Codigo);
        Assert.Equal(200, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(semSeguro, seguro: "Nenhum"))).Codigo);

        // Valor do seguro negativo não é aceito no cadastro, e a edição muda o valor do carro.
        Assert.Equal(400, (await c.Api.Post("/api/automoveis", Automovel(c.GrupoId, seguroDiaria: -1m))).Codigo);
        Assert.Equal(200, (await c.Api.Put($"/api/automoveis/{semSeguro}", Automovel(c.GrupoId, seguroDiaria: 12m))).Codigo);
        Assert.Equal(12m, (await cliente.Get($"/api/catalogo/{semSeguro}")).Corpo.GetProperty("seguroDiaria").GetDecimal());
    }

    [Fact]
    public async Task Detalhe_do_carro_mostra_os_planos_e_o_seguro_do_proprio_carro()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente(comDados: false);

        var r = await cliente.Get($"/api/catalogo/{carro}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(150m, r.Corpo.GetProperty("diario").GetProperty("precoDiaria").GetDecimal());
        Assert.Equal(100, r.Corpo.GetProperty("controlado").GetProperty("kmDisponiveisPorDia").GetInt32());
        Assert.Equal(300m, r.Corpo.GetProperty("livre").GetProperty("precoDiaria").GetDecimal());
        Assert.Equal(30m, r.Corpo.GetProperty("seguroDiaria").GetDecimal());
        Assert.Equal(1000m, r.Corpo.GetProperty("valorCaucao").GetDecimal());

        // Carro de grupo sem plano: sem opções de plano. E carro inexistente: 404.
        var semPlano = await c.NovoAutomovel(grupo: c.GrupoSemPlanoId);
        var detalheSemPlano = await cliente.Get($"/api/catalogo/{semPlano}");
        Assert.Equal(System.Text.Json.JsonValueKind.Null, detalheSemPlano.Corpo.GetProperty("diario").ValueKind);
        Assert.Equal(404, (await cliente.Get($"/api/catalogo/{Guid.NewGuid()}")).Codigo);
        Assert.Equal(403, (await c.Api.Get($"/api/catalogo/{carro}")).Codigo);
    }

    // ---------------- pedido de reserva ----------------

    [Fact]
    public async Task Estimativa_usa_a_mesma_conta_do_aluguel()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente(comDados: false);

        var r = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro));
        Assert.Equal(200, r.Codigo);
        Assert.Equal(3, r.Corpo.GetProperty("dias").GetInt32());
        Assert.Equal(450m, r.Numero("valorDiarias"));
        Assert.Equal(1000m, r.Numero("valorCaucao"));
        Assert.Equal(1450m, r.Numero("valorTotalPrevisto"));

        var comSeguro = await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, seguro: "Contratado"));
        Assert.Equal(90m, comSeguro.Numero("valorSeguro"));
        Assert.Equal(1540m, comSeguro.Numero("valorTotalPrevisto"));
    }

    [Fact]
    public async Task Pedido_exige_dados_cnh_valida_e_recusa_datas_carro_e_seguro_invalidos()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var semPlano = await c.NovoAutomovel(grupo: c.GrupoSemPlanoId);

        var (semDados, _, _) = await NovoCliente(comDados: false);
        var r = await semDados.Post("/api/minhas-reservas", Pedido(carro));
        Assert.Equal(400, r.Codigo);
        Assert.Contains("Meus dados", r.Bruto);

        var (cnhCurta, _, _) = await NovoCliente(validadeCnhEmDias: 2);
        var curta = await cnhCurta.Post("/api/minhas-reservas", Pedido(carro, devolucao: 5));
        Assert.Equal(400, curta.Codigo);
        Assert.Contains("CNH", curta.Bruto);

        var (cliente, _, _) = await NovoCliente();
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas", Pedido(carro, retirada: 5, devolucao: 2))).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas", Pedido(carro, retirada: -30, devolucao: -25))).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas", Pedido(carro, devolucao: 500))).Codigo);
        Assert.Equal(404, (await cliente.Post("/api/minhas-reservas", Pedido(Guid.NewGuid()))).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas", Pedido(semPlano))).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/minhas-reservas", Pedido(carro, plano: "Mensal"))).Codigo);

        // Locadora e funcionário não pedem reservas.
        Assert.Equal(403, (await c.Api.Post("/api/minhas-reservas", Pedido(carro))).Codigo);
    }

    [Fact]
    public async Task Pedido_chega_para_a_locadora_com_os_dados_do_cliente()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();
        var (cliente, email, cpf) = await NovoCliente();

        var criado = await cliente.Post("/api/minhas-reservas", Pedido(carro, seguro: "Contratado"));
        Assert.Equal(201, criado.Codigo);

        var meus = await cliente.Get("/api/minhas-reservas");
        Assert.Equal(1, meus.Registros.GetArrayLength());
        Assert.Equal("Pendente", meus.Registros[0].GetProperty("status").GetString());

        var recebidas = await c.Api.Get("/api/reservas");
        Assert.Equal(200, recebidas.Codigo);
        var reserva = recebidas.Registros[0];
        Assert.Equal(email, reserva.GetProperty("clienteEmail").GetString());
        Assert.Equal(cpf, reserva.GetProperty("clienteCpf").GetString());
        Assert.Equal("Beatriz Lima", reserva.GetProperty("clienteNome").GetString());
        Assert.Equal(1540m, reserva.GetProperty("valorTotalPrevisto").GetDecimal());

        Assert.Equal(1, (await c.Api.Get("/api/reservas/pendentes/total")).Corpo.GetProperty("total").GetInt32());

        // O funcionário também vê; o cliente não acessa as rotas da locadora.
        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(1, (await c.Api.Get("/api/reservas?status=Pendente")).Registros.GetArrayLength());
        Assert.Equal(403, (await cliente.Get("/api/reservas")).Codigo);
    }

    [Fact]
    public async Task Locadora_so_ve_os_pedidos_dos_proprios_carros_e_nao_age_nos_de_outra()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carro = await a.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente();

        await cliente.Post("/api/minhas-reservas", Pedido(carro));
        var id = (await a.Api.Get("/api/reservas")).Registros[0].GetProperty("id").GetGuid();

        Assert.Equal(0, (await b.Api.Get("/api/reservas")).Registros.GetArrayLength());
        Assert.Equal(404, (await b.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 0 })).Codigo);
        Assert.Equal(404, (await b.Api.Post($"/api/reservas/{id}/recusar", new { motivo = "x" })).Codigo);
    }

    [Fact]
    public async Task Pedido_repetido_para_o_mesmo_carro_e_periodo_e_recusado_e_ha_limite_de_pendentes()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente();

        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro))).Codigo);
        Assert.Equal(409, (await cliente.Post("/api/minhas-reservas", Pedido(carro, retirada: 2, devolucao: 6))).Codigo);

        // Mesmo carro em outro período pode.
        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro, retirada: 10, devolucao: 12))).Codigo);

        // Até 5 pendentes ao mesmo tempo.
        for (int i = 0; i < 3; i++)
            Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(await c.NovoAutomovel()))).Codigo);

        var sexta = await cliente.Post("/api/minhas-reservas", Pedido(await c.NovoAutomovel()));
        Assert.Equal(422, sexta.Codigo);
        Assert.Contains("5 pedidos", sexta.Bruto);
    }

    [Fact]
    public async Task Carro_alugado_no_periodo_nao_pode_ser_reservado()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, saida: 1, retorno: 5, seguro: "Nenhum"))).Codigo);
        var (cliente, _, _) = await NovoCliente();

        Assert.Equal(409, (await cliente.Post("/api/minhas-reservas", Pedido(carro, retirada: 2, devolucao: 4))).Codigo);
        Assert.Equal(409, (await cliente.Post("/api/minhas-reservas/calcular", Pedido(carro, retirada: 2, devolucao: 4))).Codigo);
        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro, retirada: 8, devolucao: 9))).Codigo);
    }

    // ---------------- cancelar ----------------

    [Fact]
    public async Task Cliente_cancela_o_pedido_pendente_uma_vez_e_ninguem_cancela_o_dos_outros()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente();
        var (outro, _, _) = await NovoCliente();

        await cliente.Post("/api/minhas-reservas", Pedido(carro));
        var id = (await cliente.Get("/api/minhas-reservas")).Registros[0].GetProperty("id").GetGuid();

        Assert.Equal(404, (await outro.Post($"/api/minhas-reservas/{id}/cancelar")).Codigo);

        var cancelada = await cliente.Post($"/api/minhas-reservas/{id}/cancelar");
        Assert.Equal(200, cancelada.Codigo);
        Assert.Equal("Cancelada", cancelada.Texto("status"));

        Assert.Equal(422, (await cliente.Post($"/api/minhas-reservas/{id}/cancelar")).Codigo);

        // A locadora não consegue aprovar um pedido cancelado.
        Assert.Equal(422, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 0 })).Codigo);

        // Cancelado, o mesmo período volta a poder ser pedido.
        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro))).Codigo);
    }

    // ---------------- recusar ----------------

    [Fact]
    public async Task Locadora_recusa_com_motivo_o_cliente_ve_e_recebe_e_mail()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, email, _) = await NovoCliente();

        await cliente.Post("/api/minhas-reservas", Pedido(carro));
        var id = (await c.Api.Get("/api/reservas")).Registros[0].GetProperty("id").GetGuid();

        var recusada = await c.Api.Post($"/api/reservas/{id}/recusar", new { motivo = "Carro em manutenção nesse período." });
        Assert.Equal(200, recusada.Codigo);

        var minha = (await cliente.Get("/api/minhas-reservas")).Registros[0];
        Assert.Equal("Recusada", minha.GetProperty("status").GetString());
        Assert.Equal("Carro em manutenção nesse período.", minha.GetProperty("motivoRecusa").GetString());

        var enviado = Emails.Enviados.Last(e => e.Para == email);
        Assert.Contains("não foi aprovada", enviado.Assunto);
        Assert.Contains("Carro em manutenção", enviado.Corpo);

        Assert.Equal(422, (await c.Api.Post($"/api/reservas/{id}/recusar", new { motivo = "de novo" })).Codigo);
        Assert.Equal(422, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 0 })).Codigo);
    }

    // ---------------- aprovar ----------------

    [Fact]
    public async Task Aprovar_cria_cliente_condutor_e_aluguel_na_locadora_e_avisa_o_cliente()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();
        await c.Api.Put("/api/configuracoes/locadora", new { nomeFantasia = "Serra Aluguel", telefone = "(49) 99113-0685" });

        var (cliente, email, cpf) = await NovoCliente();
        await cliente.Post("/api/minhas-reservas", Pedido(carro, seguro: "Contratado"));
        var id = (await c.Api.Get("/api/reservas")).Registros[0].GetProperty("id").GetGuid();

        Assert.Equal(400, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = -5 })).Codigo);

        // O funcionário aprova.
        c.Api.UsarToken(c.TokenFuncionario);
        var aprovada = await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 12000 });
        Assert.Equal(200, aprovada.Codigo);
        Assert.Equal("Aprovada", aprovada.Texto("status"));
        var aluguelId = aprovada.Corpo.GetProperty("aluguelId").GetGuid();

        // Virou um aluguel em aberto, com as datas, o plano, o seguro e a quilometragem do pedido.
        var aluguel = await c.Api.Get($"/api/alugueis/{aluguelId}");
        Assert.Equal("EmAberto", aluguel.Texto("status"));
        Assert.Equal("Beatriz Lima", aluguel.Texto("nomeCliente"));
        Assert.Equal(Dia(1), aluguel.Texto("dataSaida"));
        Assert.Equal(Dia(4), aluguel.Texto("dataRetornoPrevista"));
        Assert.Equal(12000, aluguel.Corpo.GetProperty("kmInicial").GetInt32());
        Assert.Equal("Contratado", aluguel.Texto("tipoSeguro"));
        Assert.Equal(1540m, aluguel.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());

        // O cliente e o condutor passaram a existir na locadora, com o CPF do pedido.
        var clientes = await c.Api.Get("/api/clientes?tipo=PessoaFisica");
        Assert.Contains(cpf, clientes.Registros.EnumerateArray().Select(x => x.GetProperty("cpf").GetString()));
        Assert.Contains("Beatriz Lima", (await c.Api.Get("/api/condutores")).Registros.EnumerateArray().Select(x => x.GetProperty("nome").GetString()));

        // O cliente vê "Aprovada" e o telefone da locadora, e recebeu o e-mail.
        var minha = (await cliente.Get("/api/minhas-reservas")).Registros[0];
        Assert.Equal("Aprovada", minha.GetProperty("status").GetString());
        Assert.Equal("(49) 99113-0685", minha.GetProperty("telefoneLocadora").GetString());
        Assert.Equal("Serra Aluguel", minha.GetProperty("nomeLocadora").GetString());

        var enviado = Emails.Enviados.Last(e => e.Para == email);
        Assert.Contains("aprovou", enviado.Corpo);
        Assert.Contains("(49) 99113-0685", enviado.Corpo);

        // O carro está ocupado e o pedido não pode ser aprovado de novo nem cancelado pelo cliente.
        Assert.Equal(422, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 0 })).Codigo);
        Assert.Equal(422, (await cliente.Post($"/api/minhas-reservas/{id}/cancelar")).Codigo);
        Assert.Equal(409, (await (await NovoCliente()).Api.Post("/api/minhas-reservas", Pedido(carro))).Codigo);
    }

    [Fact]
    public async Task Aprovar_reaproveita_o_cliente_com_o_mesmo_cpf()
    {
        var c = await Criar(fabrica);
        var carro1 = await c.NovoAutomovel();
        var carro2 = await c.NovoAutomovel();
        var (cliente, _, cpf) = await NovoCliente();

        await cliente.Post("/api/minhas-reservas", Pedido(carro1));
        await cliente.Post("/api/minhas-reservas", Pedido(carro2));

        foreach (var reserva in (await c.Api.Get("/api/reservas")).Registros.EnumerateArray().ToList())
            Assert.Equal(200, (await c.Api.Post($"/api/reservas/{reserva.GetProperty("id").GetGuid()}/aprovar", new { kmInicial = 0 })).Codigo);

        var clientes = await c.Api.Get("/api/clientes?tipo=PessoaFisica");
        Assert.Equal(1, clientes.Registros.EnumerateArray().Count(x => x.GetProperty("cpf").GetString() == cpf));
        Assert.Equal(2, (await c.Api.Get("/api/alugueis?status=EmAberto")).Registros.GetArrayLength());
    }

    [Fact]
    public async Task Aprovar_falha_se_o_carro_foi_alugado_depois_do_pedido_e_o_pedido_continua_pendente()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente();

        await cliente.Post("/api/minhas-reservas", Pedido(carro));
        var id = (await c.Api.Get("/api/reservas")).Registros[0].GetProperty("id").GetGuid();
        int clientesAntes = (await c.Api.Get("/api/clientes")).Registros.GetArrayLength();

        // Antes de responder, a locadora aluga o mesmo carro no balcão.
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, saida: 2, retorno: 3, seguro: "Nenhum"))).Codigo);

        var aprovar = await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 0 });
        Assert.Equal(409, aprovar.Codigo);

        var pendente = (await c.Api.Get("/api/reservas?status=Pendente")).Registros;
        Assert.Equal(1, pendente.GetArrayLength());

        // O cliente só passa a existir quando o aluguel acontece: a aprovação que falhou não deixa cliente para trás.
        Assert.Equal(clientesAntes, (await c.Api.Get("/api/clientes")).Registros.GetArrayLength());
    }
}
