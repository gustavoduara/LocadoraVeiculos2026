using System.Text.Json;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class AluguelIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private static readonly Guid Inexistente = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ---------------- simulação de preço ----------------

    [Fact]
    public async Task Simulacao_calcula_diarias_taxas_seguro_e_caucao()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        var r = await c.Api.Post("/api/alugueis/calcular",
            PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaDiariaId, c.TaxaFixaId]));

        Assert.Equal(200, r.Codigo);

        var previsto = r.Corpo.GetProperty("previsto");

        Assert.Equal(3, previsto.GetProperty("dias").GetInt32());
        Assert.Equal(450m, previsto.GetProperty("valorDiarias").GetDecimal());
        Assert.Equal(110m, previsto.GetProperty("valorTaxas").GetDecimal());
        Assert.Equal(90m, previsto.GetProperty("valorSeguro").GetDecimal());
        Assert.Equal(1000m, previsto.GetProperty("valorCaucao").GetDecimal());
        Assert.Equal(1650m, previsto.GetProperty("valorTotalPrevisto").GetDecimal());
    }

    [Fact]
    public async Task Simulacao_no_mesmo_dia_cobra_uma_diaria()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        var r = await c.Api.Post("/api/alugueis/calcular",
            PedidoAluguel(c.CondutorId, carro, "Livre", retorno: 0, seguro: "Nenhum"));

        Assert.Equal(1, r.Corpo.GetProperty("previsto").GetProperty("dias").GetInt32());
        Assert.Equal(1300m, r.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());
    }

    [Fact]
    public async Task Simulacao_com_automovel_de_outra_empresa_nao_encontra()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carroDeB = await b.NovoAutomovel();

        var r = await a.Api.Post("/api/alugueis/calcular", PedidoAluguel(a.CondutorId, carroDeB));

        Assert.Equal(404, r.Codigo);
    }

    // ---------------- cadastro ----------------

    [Fact]
    public async Task Exige_autenticacao()
    {
        var c = await Criar(fabrica);
        c.Api.UsarToken(null);

        var r = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, Guid.NewGuid()));

        Assert.Equal(401, r.Codigo);
    }

    [Fact]
    public async Task Funcionario_registra_aluguel_e_cliente_vem_do_condutor()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();
        c.Api.UsarToken(c.TokenFuncionario);

        var criado = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaDiariaId, c.TaxaFixaId]));

        Assert.Equal(201, criado.Codigo);

        var r = await c.Api.Get($"/api/alugueis/{criado.Id}");

        Assert.Equal("EmAberto", r.Texto("status"));
        Assert.Equal("Transportes Alfa", r.Texto("nomeCliente"));
        Assert.Equal(2, r.Corpo.GetProperty("taxas").GetArrayLength());
        Assert.Equal(1650m, r.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());
    }

    [Fact]
    public async Task Rejeita_dados_invalidos()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        async Task<int> Codigo(object corpo) => (await c.Api.Post("/api/alugueis", corpo)).Codigo;

        Assert.Equal(400, await Codigo(PedidoAluguel(c.CondutorId, carro, saida: 5, retorno: 2)));
        Assert.Equal(400, await Codigo(PedidoAluguel(c.CondutorId, carro, retorno: 400)));
        Assert.Equal(400, await Codigo(PedidoAluguel(c.CondutorId, carro, kmInicial: -1)));
        Assert.Equal(400, await Codigo(PedidoAluguel(c.CondutorId, carro, kmInicial: 5_000_000)));
        Assert.Equal(404, await Codigo(PedidoAluguel(Inexistente, carro)));
        Assert.Equal(404, await Codigo(PedidoAluguel(c.CondutorId, Inexistente)));
        Assert.Equal(404, await Codigo(PedidoAluguel(c.CondutorId, carro, taxas: [Inexistente])));
    }

    [Fact]
    public async Task Automovel_de_grupo_sem_plano_de_cobranca_e_recusado()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel(grupo: c.GrupoSemPlanoId);

        var r = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro));

        Assert.Equal(400, r.Codigo);
        Assert.Contains("plano de cobran", r.Bruto);
    }

    [Fact]
    public async Task Cnh_que_vence_antes_do_retorno_e_recusada_mas_no_ultimo_dia_e_aceita()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        var vencida = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorCnhCurtaId, carro, retorno: 10));

        Assert.Equal(400, vencida.Codigo);
        Assert.Contains("CNH", vencida.Bruto);

        var noLimite = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorCnhCurtaId, carro, retorno: 5));

        Assert.Equal(201, noLimite.Codigo);
    }

    [Fact]
    public async Task Seguro_sem_preco_configurado_barra_o_aluguel()
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var token = await RegistrarEmpresa(api);
        api.UsarToken(token);

        var grupo = (await api.Post("/api/grupos-veiculos", new { nome = "Compacto" })).Id;
        await api.Post("/api/planos-cobranca", new
        {
            grupoVeiculosId = grupo,
            diario = new { precoDiaria = 100m, precoPorKm = 1m },
            controlado = new { kmDisponiveis = 50, precoDiaria = 120m, precoPorKmExtrapolado = 2m },
            livre = new { precoDiaria = 200m }
        });
        var carro = (await api.Post("/api/automoveis", Automovel(grupo))).Id;
        var cliente = await SemearClientePj(fabrica, token, "Beta");
        var condutor = (await api.Post("/api/condutores", Condutor(cliente, "Joao", Dia(1000)))).Id;

        var semSeguro = await api.Post("/api/alugueis", PedidoAluguel(condutor, carro, "Livre", retorno: 1, seguro: "Nenhum"));
        var comSeguro = await api.Post("/api/alugueis", PedidoAluguel(condutor, carro, "Livre", saida: 5, retorno: 6, seguro: "Terceiros"));

        Assert.Equal(201, semSeguro.Codigo);
        Assert.Equal(400, comSeguro.Codigo);
    }

    // ---------------- disponibilidade ----------------

    [Fact]
    public async Task Mesmo_carro_em_periodo_sobreposto_da_conflito_mas_depois_do_periodo_pode()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro))).Codigo);

        Assert.Equal(409, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, saida: 2, retorno: 6))).Codigo);
        Assert.Equal(409, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, saida: 3, retorno: 5))).Codigo);
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, saida: 4, retorno: 6))).Codigo);
    }

    [Fact]
    public async Task Carro_com_aluguel_vencido_e_nao_devolvido_continua_ocupado_ate_a_devolucao()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        var atrasado = await c.Api.Post("/api/alugueis",
            PedidoAluguel(c.CondutorPfId, carro, saida: -10, retorno: -5, seguro: "Nenhum"));
        Assert.Equal(201, atrasado.Codigo);

        var novo = PedidoAluguel(c.CondutorPfId, carro, saida: 0, retorno: 2, seguro: "Nenhum");

        Assert.Equal(409, (await c.Api.Post("/api/alugueis", novo)).Codigo);

        var devolucao = await c.Api.Post($"/api/alugueis/{atrasado.Id}/devolucao",
            new { dataDevolucao = Dia(0), kmFinal = 1000, tanqueCheio = true });
        Assert.Equal(200, devolucao.Codigo);

        Assert.Equal(201, (await c.Api.Post("/api/alugueis", novo)).Codigo);
    }

    // ---------------- edição ----------------

    [Fact]
    public async Task Edita_o_proprio_aluguel_sem_conflitar_consigo_e_recalcula()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var criado = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaDiariaId, c.TaxaFixaId]));

        var livre = await c.Api.Put($"/api/alugueis/{criado.Id}",
            PedidoAluguel(c.CondutorId, carro, "Livre", seguro: "Terceiros", taxas: [c.TaxaFixaId]));

        Assert.Equal(200, livre.Codigo);
        Assert.Equal(2010m, livre.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());

        var diario = await c.Api.Put($"/api/alugueis/{criado.Id}", PedidoAluguel(c.CondutorId, carro));

        Assert.Equal(200, diario.Codigo);
        Assert.Equal(1000m + 450m + 90m, diario.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());
        Assert.Empty(diario.Corpo.GetProperty("taxas").EnumerateArray());
    }

    [Fact]
    public async Task Editar_para_periodo_ocupado_por_outro_aluguel_da_conflito()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var a = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro));
        await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, saida: 4, retorno: 6));

        var r = await c.Api.Put($"/api/alugueis/{a.Id}", PedidoAluguel(c.CondutorId, carro, saida: 4, retorno: 6));

        Assert.Equal(409, r.Codigo);
    }

    [Fact]
    public async Task Outra_empresa_nao_ve_edita_devolve_nem_exclui()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carro = await a.NovoAutomovel();
        var criado = await a.Api.Post("/api/alugueis", PedidoAluguel(a.CondutorId, carro));

        Assert.Equal(404, (await b.Api.Get($"/api/alugueis/{criado.Id}")).Codigo);
        Assert.Equal(404, (await b.Api.Put($"/api/alugueis/{criado.Id}", PedidoAluguel(b.CondutorId, carro))).Codigo);
        Assert.Equal(404, (await b.Api.Post($"/api/alugueis/{criado.Id}/devolucao",
            new { dataDevolucao = Dia(3), kmFinal = 1200, tanqueCheio = true })).Codigo);
        Assert.Equal(404, (await b.Api.Delete($"/api/alugueis/{criado.Id}")).Codigo);

        Assert.Empty((await b.Api.Get("/api/alugueis")).Registros.EnumerateArray());
        Assert.Single((await a.Api.Get("/api/alugueis")).Registros.EnumerateArray());
    }

    // ---------------- bloqueios entre módulos ----------------

    [Fact]
    public async Task Cadastros_ligados_a_aluguel_em_aberto_nao_podem_ser_editados_nem_excluidos()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();

        c.Api.UsarToken(c.TokenFuncionario);
        var aluguel = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaDiariaId, c.TaxaFixaId]));
        Assert.Equal(201, aluguel.Codigo);
        c.Api.UsarToken(c.TokenEmpresa);

        Assert.Equal(422, (await c.Api.Put($"/api/automoveis/{carro}", Automovel(c.GrupoId, placa: "ZZZ9999"))).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/automoveis/{carro}")).Codigo);
        Assert.Equal(422, (await c.Api.Put($"/api/taxas-servicos/{c.TaxaDiariaId}", new { nome = "GPS", preco = 99m, tipoCobranca = "Diario" })).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/taxas-servicos/{c.TaxaDiariaId}")).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/condutores/{c.CondutorId}")).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/planos-cobranca/{c.PlanoId}")).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/funcionarios/{c.FuncionarioId}")).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/clientes/{c.ClientePjId}")).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/alugueis/{aluguel.Id}")).Codigo);
    }

    // ---------------- devolução ----------------

    [Fact]
    public async Task Devolucao_no_prazo_plano_diario_cobra_km_e_conclui()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();
        var aluguel = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaDiariaId, c.TaxaFixaId]));
        var corpo = new { dataDevolucao = Dia(3), kmFinal = 1200, tanqueCheio = true };

        var simulacao = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao/calcular", corpo);

        Assert.Equal(200, simulacao.Codigo);
        Assert.Equal(200, simulacao.Corpo.GetProperty("kmRodados").GetInt32());
        Assert.Equal(300m, simulacao.Numero("valorKm"));
        Assert.Equal(0m, simulacao.Numero("valorMulta"));
        Assert.Equal(1950m, simulacao.Numero("valorFinal"));

        c.Api.UsarToken(c.TokenFuncionario);
        var devolvido = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao", corpo);

        Assert.Equal(200, devolvido.Codigo);
        Assert.Equal("Concluido", devolvido.Texto("status"));
        Assert.Equal(1950m, devolvido.Corpo.GetProperty("devolucao").GetProperty("final").GetProperty("valorFinal").GetDecimal());

        Assert.Equal(422, (await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao", corpo)).Codigo);
        Assert.Equal(422, (await c.Api.Put($"/api/alugueis/{aluguel.Id}", PedidoAluguel(c.CondutorId, carro))).Codigo);
    }

    [Fact]
    public async Task Devolucao_invalida_e_recusada()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var aluguel = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro));

        var antesDaSaida = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao",
            new { dataDevolucao = Dia(-1), kmFinal = 1200, tanqueCheio = true });
        var kmMenor = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao",
            new { dataDevolucao = Dia(3), kmFinal = 900, tanqueCheio = true });

        Assert.Equal(400, antesDaSaida.Codigo);
        Assert.Equal(400, kmMenor.Codigo);
    }

    [Fact]
    public async Task Devolucao_atrasada_no_plano_controlado_com_tanque_vazio_soma_km_combustivel_e_multa()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel("Diesel", 60);
        var aluguel = await c.Api.Post("/api/alugueis",
            PedidoAluguel(c.CondutorPfId, carro, "Controlado", saida: 20, retorno: 22, kmInicial: 0, seguro: "Nenhum"));

        var r = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao",
            new { dataDevolucao = Dia(23), kmFinal = 300, tanqueCheio = false });

        Assert.Equal(200, r.Codigo);

        var final = r.Corpo.GetProperty("devolucao").GetProperty("final");

        Assert.True(final.GetProperty("atrasado").GetBoolean());
        Assert.Equal(250m, final.GetProperty("valorKm").GetDecimal());
        Assert.Equal(65m, final.GetProperty("valorMulta").GetDecimal());
        Assert.Equal(330m, final.GetProperty("valorCombustivel").GetDecimal());
        Assert.Equal(2045m, final.GetProperty("valorFinal").GetDecimal());
    }

    [Fact]
    public async Task Tanque_nao_cheio_sem_preco_do_combustivel_configurado_e_recusado()
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var token = await RegistrarEmpresa(api);
        api.UsarToken(token);

        var grupo = (await api.Post("/api/grupos-veiculos", new { nome = "Compacto" })).Id;
        await api.Post("/api/planos-cobranca", new
        {
            grupoVeiculosId = grupo,
            diario = new { precoDiaria = 100m, precoPorKm = 1m },
            controlado = new { kmDisponiveis = 50, precoDiaria = 120m, precoPorKmExtrapolado = 2m },
            livre = new { precoDiaria = 200m }
        });
        var carro = (await api.Post("/api/automoveis", Automovel(grupo))).Id;
        var cliente = await SemearClientePj(fabrica, token, "Beta");
        var condutor = (await api.Post("/api/condutores", Condutor(cliente, "Joao", Dia(1000)))).Id;
        var aluguel = await api.Post("/api/alugueis", PedidoAluguel(condutor, carro, "Livre", retorno: 1, seguro: "Nenhum"));

        var vazio = await api.Post($"/api/alugueis/{aluguel.Id}/devolucao", new { dataDevolucao = Dia(1), kmFinal = 1010, tanqueCheio = false });
        var cheio = await api.Post($"/api/alugueis/{aluguel.Id}/devolucao", new { dataDevolucao = Dia(1), kmFinal = 1010, tanqueCheio = true });

        Assert.Equal(400, vazio.Codigo);
        Assert.Equal(200, cheio.Codigo);
    }

    // ---------------- congelamento, listagem e exclusão ----------------

    [Fact]
    public async Task Mudar_precos_depois_nao_altera_aluguel_concluido()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var aluguel = await c.Api.Post("/api/alugueis",
            PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaFixaId]));
        var devolvido = await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao", new { dataDevolucao = Dia(3), kmFinal = 1000, tanqueCheio = true });
        var totalAntes = devolvido.Corpo.GetProperty("devolucao").GetProperty("final").GetProperty("valorFinal").GetDecimal();

        await c.Api.Put($"/api/taxas-servicos/{c.TaxaFixaId}", new { nome = "Cadeirinha", preco = 500m, tipoCobranca = "Fixo" });
        await c.Api.Put("/api/configuracoes", new { precoGasolina = 9m, precoGas = 9m, precoDiesel = 9m, precoAlcool = 9m, seguroClienteDiaria = 99m, seguroTerceirosDiaria = 99m });

        var depois = await c.Api.Get($"/api/alugueis/{aluguel.Id}");

        Assert.Equal(totalAntes, depois.Corpo.GetProperty("devolucao").GetProperty("final").GetProperty("valorFinal").GetDecimal());
    }

    [Fact]
    public async Task Lista_filtra_por_situacao_e_so_concluido_pode_ser_excluido()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro1 = await c.NovoAutomovel();
        var carro2 = await c.NovoAutomovel();

        var aberto = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro1));
        var concluido = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro2));
        await c.Api.Post($"/api/alugueis/{concluido.Id}/devolucao", new { dataDevolucao = Dia(3), kmFinal = 1000, tanqueCheio = true });

        var todos = await c.Api.Get("/api/alugueis");
        var abertos = await c.Api.Get("/api/alugueis?status=EmAberto");
        var concluidos = await c.Api.Get("/api/alugueis?status=Concluido");

        Assert.Equal(2, todos.Registros.GetArrayLength());
        Assert.Single(abertos.Registros.EnumerateArray());
        Assert.Equal(aberto.Id, abertos.Registros[0].GetProperty("id").GetGuid());
        Assert.Single(concluidos.Registros.EnumerateArray());

        Assert.Equal(422, (await c.Api.Delete($"/api/alugueis/{aberto.Id}")).Codigo);
        Assert.Equal(204, (await c.Api.Delete($"/api/alugueis/{concluido.Id}")).Codigo);
        Assert.Equal(404, (await c.Api.Get($"/api/alugueis/{concluido.Id}")).Codigo);
    }

    [Fact]
    public async Task Cadastros_sao_liberados_depois_que_o_aluguel_termina()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();

        c.Api.UsarToken(c.TokenFuncionario);
        var aluguel = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, taxas: [c.TaxaDiariaId]));
        await c.Api.Post($"/api/alugueis/{aluguel.Id}/devolucao", new { dataDevolucao = Dia(3), kmFinal = 1000, tanqueCheio = true });
        Assert.Equal(204, (await c.Api.Delete($"/api/alugueis/{aluguel.Id}")).Codigo);

        c.Api.UsarToken(c.TokenEmpresa);

        Assert.Equal(200, (await c.Api.Put($"/api/automoveis/{carro}", Automovel(c.GrupoId, placa: "ZZZ9999"))).Codigo);
        Assert.Equal(204, (await c.Api.Delete($"/api/taxas-servicos/{c.TaxaDiariaId}")).Codigo);
        Assert.Equal(204, (await c.Api.Delete($"/api/funcionarios/{c.FuncionarioId}")).Codigo);
    }

    // ---------------- cliente novo dentro do aluguel ----------------

    private static object NovoClientePessoaFisica(string nome, string? cpf = null, string validadeCnh = "") => new
    {
        tipo = "PessoaFisica", nome, email = "novo@ex.com", telefone = "(47) 99999-1234",
        cpf = cpf ?? Geradores.Cpf(), rg = "1234567", cnh = "12345678901",
        estado = "SC", cidade = "Rio do Sul", bairro = "Centro", rua = "Rua XV", numero = "10",
        validadeCnh = validadeCnh == "" ? Dia(1000) : validadeCnh
    };

    private static object NovoClientePessoaJuridica(string nome) => new
    {
        tipo = "PessoaJuridica", nome, email = "contato@empresa.com", telefone = "(47) 3333-4444",
        cnpj = Geradores.Cnpj(),
        estado = "SC", cidade = "Rio do Sul", bairro = "Centro", rua = "Rua XV", numero = "10",
        validadeCnh = Dia(1000),
        condutor = new
        {
            nome = "Carlos Motorista", email = "carlos@empresa.com", telefone = "(47) 98888-7777",
            cpf = Geradores.Cpf(), cnh = "98765432100", validadeCnh = Dia(1000)
        }
    };

    private static object PedidoComClienteNovo(Guid carro, object novoCliente, int retorno = 3) => new
    {
        automovelId = carro,
        tipoPlano = "Diario",
        dataSaida = Dia(0),
        dataRetornoPrevista = Dia(retorno),
        kmInicial = 1000,
        tipoSeguro = "Nenhum",
        taxasIds = Array.Empty<Guid>(),
        novoCliente
    };

    [Fact]
    public async Task Cliente_pessoa_fisica_nasce_junto_com_o_aluguel_e_e_o_proprio_condutor()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        int antes = (await c.Api.Get("/api/clientes")).Registros.GetArrayLength();

        var r = await c.Api.Post("/api/alugueis", PedidoComClienteNovo(carro, NovoClientePessoaFisica("Ana Nova")));
        Assert.Equal(201, r.Codigo);

        var aluguel = await c.Api.Get($"/api/alugueis/{r.Id}");
        Assert.Equal("Ana Nova", aluguel.Texto("nomeCliente"));
        Assert.Equal("Ana Nova", aluguel.Texto("nomeCondutor"));

        // O cliente e o condutor passaram a existir na locadora.
        Assert.Equal(antes + 1, (await c.Api.Get("/api/clientes")).Registros.GetArrayLength());
        var clienteId = aluguel.Corpo.GetProperty("clienteId").GetGuid();
        Assert.Equal(1, (await c.Api.Get($"/api/condutores?clienteId={clienteId}")).Registros.GetArrayLength());
    }

    [Fact]
    public async Task Cliente_pessoa_juridica_nasce_com_o_condutor_informado()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        var r = await c.Api.Post("/api/alugueis", PedidoComClienteNovo(carro, NovoClientePessoaJuridica("Empresa Nova")));
        Assert.Equal(201, r.Codigo);

        var aluguel = await c.Api.Get($"/api/alugueis/{r.Id}");
        Assert.Equal("Empresa Nova", aluguel.Texto("nomeCliente"));
        Assert.Equal("Carlos Motorista", aluguel.Texto("nomeCondutor"));
    }

    [Fact]
    public async Task Cliente_novo_so_aparece_para_a_locadora_que_fez_o_aluguel()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);
        var carro = await a.NovoAutomovel();

        var r = await a.Api.Post("/api/alugueis", PedidoComClienteNovo(carro, NovoClientePessoaFisica("Bia Exclusiva")));
        Assert.Equal(201, r.Codigo);

        static string Nomes(JsonElement registros) =>
            string.Join(",", registros.EnumerateArray().Select(x => x.GetProperty("nome").GetString()));

        Assert.Contains("Bia Exclusiva", Nomes((await a.Api.Get("/api/clientes")).Registros));
        Assert.DoesNotContain("Bia Exclusiva", Nomes((await b.Api.Get("/api/clientes")).Registros));
    }

    [Fact]
    public async Task Aluguel_que_falha_nao_deixa_o_cliente_novo_para_tras()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        // O carro já está alugado no período: o aluguel falha e o cliente que seria criado não pode sobrar.
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, seguro: "Nenhum"))).Codigo);
        int antes = (await c.Api.Get("/api/clientes")).Registros.GetArrayLength();

        var r = await c.Api.Post("/api/alugueis", PedidoComClienteNovo(carro, NovoClientePessoaFisica("Fantasma")));
        Assert.Equal(409, r.Codigo);
        Assert.Equal(antes, (await c.Api.Get("/api/clientes")).Registros.GetArrayLength());

        // CNH que vence antes do retorno também recusa e não cria ninguém.
        var outro = await c.NovoAutomovel();
        var vencida = await c.Api.Post("/api/alugueis",
            PedidoComClienteNovo(outro, NovoClientePessoaFisica("Sem Cnh", validadeCnh: Dia(1))));
        Assert.Equal(400, vencida.Codigo);
        Assert.Equal(antes, (await c.Api.Get("/api/clientes")).Registros.GetArrayLength());
    }

    [Fact]
    public async Task Cliente_novo_valida_documentos_duplicidade_e_a_escolha_entre_existente_e_novo()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        // CPF já cadastrado nesta locadora: é preciso escolher o cliente existente.
        string cpfDaMaria = (await c.Api.Get($"/api/clientes/{c.ClientePfId}")).Texto("cpf");
        Assert.Equal(409, (await c.Api.Post("/api/alugueis",
            PedidoComClienteNovo(carro, NovoClientePessoaFisica("Repetida", cpfDaMaria)))).Codigo);

        // CPF inválido.
        Assert.Equal(400, (await c.Api.Post("/api/alugueis",
            PedidoComClienteNovo(carro, NovoClientePessoaFisica("Cpf Ruim", "111.111.111-11")))).Codigo);

        // Pessoa jurídica sem condutor.
        var semCondutor = new
        {
            tipo = "PessoaJuridica", nome = "Sem Condutor", email = "a@a.com", telefone = "(47) 3333-4444",
            cnpj = Geradores.Cnpj(), estado = "SC", cidade = "Rio do Sul", bairro = "Centro", rua = "Rua XV",
            numero = "1", validadeCnh = Dia(1000)
        };
        Assert.Equal(400, (await c.Api.Post("/api/alugueis", PedidoComClienteNovo(carro, semCondutor))).Codigo);

        // Condutor existente e cliente novo ao mesmo tempo, ou nenhum dos dois.
        var dois = new
        {
            condutorId = c.CondutorId, automovelId = carro, tipoPlano = "Diario", dataSaida = Dia(0),
            dataRetornoPrevista = Dia(3), kmInicial = 1000, tipoSeguro = "Nenhum", taxasIds = Array.Empty<Guid>(),
            novoCliente = NovoClientePessoaFisica("Dois")
        };
        Assert.Equal(400, (await c.Api.Post("/api/alugueis", dois)).Codigo);

        var nenhum = new
        {
            automovelId = carro, tipoPlano = "Diario", dataSaida = Dia(0), dataRetornoPrevista = Dia(3),
            kmInicial = 1000, tipoSeguro = "Nenhum", taxasIds = Array.Empty<Guid>()
        };
        Assert.Equal(400, (await c.Api.Post("/api/alugueis", nenhum)).Codigo);
    }

    [Fact]
    public async Task Simulacao_funciona_com_cliente_novo_mas_editar_nao_cadastra_cliente()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        int antes = (await c.Api.Get("/api/clientes")).Registros.GetArrayLength();

        var simulacao = await c.Api.Post("/api/alugueis/calcular", PedidoComClienteNovo(carro, NovoClientePessoaFisica("So Simula")));
        Assert.Equal(200, simulacao.Codigo);
        Assert.Equal(1450m, simulacao.Corpo.GetProperty("previsto").GetProperty("valorTotalPrevisto").GetDecimal());
        Assert.Equal(antes, (await c.Api.Get("/api/clientes")).Registros.GetArrayLength());

        var existente = await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, seguro: "Nenhum"));
        var edicao = await c.Api.Put($"/api/alugueis/{existente.Id}", PedidoComClienteNovo(carro, NovoClientePessoaFisica("Na Edicao")));
        Assert.Equal(400, edicao.Codigo);
    }
}
