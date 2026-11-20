using System.Text.Json;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class CatalogoIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    // PNG de 1x1 pixel.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>Os testes da classe dividem o mesmo banco: cada um marca os próprios carros para achá-los na busca.</summary>
    private sealed record Frotas(Cenario A, Cenario B, string Marca, Guid Onix, Guid Hilux);

    private async Task<ClienteApi> NovoClienteDoCatalogo()
    {
        var api = new ClienteApi(fabrica.CreateClient());

        var r = await api.Post("/api/auth/registrar-cliente", new
        {
            nomeCompleto = "Pessoa Que Aluga",
            email = Geradores.Email("cliente"),
            senha = "Senha123",
            confirmarSenha = "Senha123"
        });

        Assert.Equal(200, r.Codigo);

        api.UsarToken(r.Texto("chave"));

        return api;
    }

    private static object Carro(Guid grupo, string marca, string modelo, int ano, string combustivel, string cor = "Branco") => new
    {
        placa = Geradores.Placa(),
        marca,
        modelo,
        cor,
        ano,
        tipoCombustivel = combustivel,
        capacidadeCombustivelLitros = 50,
        grupoVeiculosId = grupo
    };

    private static async Task Plano(ClienteApi api, Guid grupo, decimal diaria)
    {
        var r = await api.Post("/api/planos-cobranca", new
        {
            grupoVeiculosId = grupo,
            diario = new { precoDiaria = diaria, precoPorKm = 1m },
            controlado = new { kmDisponiveis = 100, precoDiaria = diaria + 40, precoPorKmExtrapolado = 2m },
            livre = new { precoDiaria = diaria + 100 }
        });
        Assert.Equal(201, r.Codigo);
    }

    /// <summary>
    /// Duas locadoras. A: Onix (2024, gasolina, R$ 80) e Mobi (2020). B: Hilux (2022, diesel, R$ 150) e Kwid (sem plano).
    /// O sufixo da marca (único por chamada) serve para filtrar só estes carros com texto=.
    /// </summary>
    private async Task<Frotas> DuasLocadoras()
    {
        string marca = "Zz" + Guid.NewGuid().ToString("N")[..8];

        var a = await Criar(fabrica);
        var b = await Criar(fabrica);

        var economicoA = (await a.Api.Post("/api/grupos-veiculos", new { nome = "Economico" })).Id;
        await Plano(a.Api, economicoA, 80m);
        var onix = (await a.Api.Post("/api/automoveis", Carro(economicoA, marca, "Onix", 2024, "Gasolina", "Prata"))).Id;
        await a.Api.Post("/api/automoveis", Carro(economicoA, marca, "Mobi", 2020, "Gasolina", "Vermelho"));

        // B usa o grupo "Utilitario" do cenário (plano com menor diária 150) e o "Sem plano".
        var hilux = (await b.Api.Post("/api/automoveis", Carro(b.GrupoId, marca, "Hilux", 2022, "Diesel", "Preto"))).Id;
        await b.Api.Post("/api/automoveis", Carro(b.GrupoSemPlanoId, marca, "Kwid", 2021, "Gasolina"));

        return new Frotas(a, b, marca, onix, hilux);
    }

    private static List<string> Modelos(Resposta r) =>
        r.Corpo.GetProperty("registros").EnumerateArray().Select(x => x.GetProperty("modelo").GetString()!).ToList();

    // ---------------- conta de cliente ----------------

    [Fact]
    public async Task Cliente_se_cadastra_entra_e_nao_alcanca_as_rotas_das_locadoras()
    {
        var api = await NovoClienteDoCatalogo();

        Assert.Equal(403, (await api.Get("/api/grupos-veiculos")).Codigo);
        Assert.Equal(403, (await api.Get("/api/automoveis")).Codigo);
        Assert.Equal(403, (await api.Get("/api/alugueis")).Codigo);
        Assert.Equal(403, (await api.Get("/api/clientes")).Codigo);
        Assert.Equal(403, (await api.Get("/api/configuracoes")).Codigo);
        Assert.Equal(403, (await api.Get("/api/funcionarios")).Codigo);

        Assert.Equal(200, (await api.Get("/api/catalogo")).Codigo);
    }

    [Fact]
    public async Task Login_do_cliente_devolve_o_cargo_cliente()
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var email = Geradores.Email("login");

        await api.Post("/api/auth/registrar-cliente", new { nomeCompleto = "Maria", email, senha = "Senha123", confirmarSenha = "Senha123" });

        var login = await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" });

        Assert.Equal(200, login.Codigo);
        Assert.Equal("Cliente", login.Corpo.GetProperty("usuarioAutenticado").GetProperty("cargo").GetString());
    }

    [Fact]
    public async Task Cadastro_de_cliente_recusa_senha_fraca_e_email_repetido()
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var email = Geradores.Email("dup");

        var fraca = await api.Post("/api/auth/registrar-cliente", new { nomeCompleto = "Maria", email, senha = "semmaiuscula1", confirmarSenha = "semmaiuscula1" });
        Assert.Equal(400, fraca.Codigo);
        Assert.Contains("letra maiúscula", fraca.Bruto);

        var corpo = new { nomeCompleto = "Maria", email, senha = "Senha123", confirmarSenha = "Senha123" };
        Assert.Equal(200, (await api.Post("/api/auth/registrar-cliente", corpo)).Codigo);
        Assert.InRange((await api.Post("/api/auth/registrar-cliente", corpo)).Codigo, 400, 409);
    }

    [Fact]
    public async Task Catalogo_exige_login_e_e_so_do_cliente()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        var anonimo = new ClienteApi(fabrica.CreateClient());
        Assert.Equal(401, (await anonimo.Get("/api/catalogo")).Codigo);

        Assert.Equal(403, (await c.Api.Get("/api/catalogo")).Codigo);
        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(403, (await c.Api.Get("/api/catalogo")).Codigo);
    }

    // ---------------- busca ----------------

    [Fact]
    public async Task Catalogo_junta_os_carros_de_todas_as_locadoras_sem_expor_a_placa()
    {
        var f = await DuasLocadoras();
        var cliente = await NovoClienteDoCatalogo();

        var r = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto={f.Marca}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(4, r.Corpo.GetProperty("total").GetInt32());
        Assert.Equal(["Hilux", "Kwid", "Mobi", "Onix"], Modelos(r).Order().ToList());

        // Os carros de duas locadoras diferentes aparecem juntos, cada um com o nome da sua.
        var locadoras = r.Corpo.GetProperty("registros").EnumerateArray().Select(x => x.GetProperty("locadoraId").GetGuid()).Distinct().Count();
        Assert.Equal(2, locadoras);

        Assert.DoesNotContain("placa", r.Bruto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Texto_categoria_combustivel_ano_e_preco_filtram_o_resultado()
    {
        var f = await DuasLocadoras();
        var cliente = await NovoClienteDoCatalogo();

        async Task<List<string>> Busca(string consulta)
        {
            var r = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto={f.Marca}&" + consulta);
            Assert.Equal(200, r.Codigo);

            return Modelos(r).Order().ToList();
        }

        Assert.Equal(["Onix"], await Busca("categoria=Economico&anoMinimo=2023"));
        Assert.Equal(["Mobi", "Onix"], await Busca("categoria=Economico"));
        Assert.Equal(["Hilux"], await Busca("combustivel=Diesel"));
        Assert.Equal(["Kwid", "Mobi", "Onix"], await Busca("combustivel=Gasolina"));
        Assert.Equal(["Hilux", "Onix"], await Busca("anoMinimo=2022"));

        // Preço: Onix e Mobi custam R$ 80, Hilux R$ 150, e o Kwid (sem plano) não tem preço, então não entra.
        Assert.Equal(["Mobi", "Onix"], await Busca("diariaMaxima=100"));
        Assert.Equal(["Hilux", "Mobi", "Onix"], await Busca("diariaMaxima=1000"));

        // Texto livre: marca, modelo e cor, em qualquer ordem e sem diferenciar maiúsculas.
        var porCor = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto=PRATA {f.Marca}");
        Assert.Equal(["Onix"], Modelos(porCor));
    }

    [Fact]
    public async Task Filtra_pela_locadora_e_ordena_por_preco_e_por_ano()
    {
        var f = await DuasLocadoras();
        var cliente = await NovoClienteDoCatalogo();

        var filtros = await cliente.Get("/api/catalogo/filtros");
        Assert.Equal(200, filtros.Codigo);
        Assert.Contains("Economico", filtros.Corpo.GetProperty("categorias").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains(f.Marca, filtros.Corpo.GetProperty("marcas").EnumerateArray().Select(x => x.GetString()));
        Assert.True(filtros.Corpo.GetProperty("locadoras").GetArrayLength() >= 2);

        var idDeB = (await cliente.Get($"/api/catalogo?texto={f.Marca} hilux")).Corpo.GetProperty("registros")[0].GetProperty("locadoraId").GetGuid();

        var daB = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto={f.Marca}&locadoraId={idDeB}");
        Assert.Equal(["Hilux", "Kwid"], Modelos(daB).Order().ToList());

        // Menor preço primeiro; sem preço por último.
        var menor = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto={f.Marca}&ordenacao=MenorPreco");
        Assert.Equal("Kwid", Modelos(menor).Last());
        Assert.Equal("Hilux", Modelos(menor)[2]);

        var maior = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto={f.Marca}&ordenacao=MaiorPreco");
        Assert.Equal("Hilux", Modelos(maior).First());
        Assert.Equal("Kwid", Modelos(maior).Last());

        var maisNovo = await cliente.Get($"/api/catalogo?tamanhoPagina=50&texto={f.Marca}&ordenacao=MaisNovo");
        var anos = maisNovo.Corpo.GetProperty("registros").EnumerateArray().Select(x => x.GetProperty("ano").GetInt32()).ToList();
        Assert.Equal(anos.OrderByDescending(x => x).ToList(), anos);
    }

    [Fact]
    public async Task Pagina_o_resultado()
    {
        var f = await DuasLocadoras();
        var cliente = await NovoClienteDoCatalogo();

        var primeira = await cliente.Get($"/api/catalogo?texto={f.Marca}&tamanhoPagina=3&pagina=1&ordenacao=Marca");
        var segunda = await cliente.Get($"/api/catalogo?texto={f.Marca}&tamanhoPagina=3&pagina=2&ordenacao=Marca");

        Assert.Equal(4, primeira.Corpo.GetProperty("total").GetInt32());
        Assert.Equal(3, primeira.Corpo.GetProperty("registros").GetArrayLength());
        Assert.Equal(1, segunda.Corpo.GetProperty("registros").GetArrayLength());
    }

    // ---------------- período e disponibilidade ----------------

    [Fact]
    public async Task Periodo_calcula_o_total_e_esconde_ou_marca_o_carro_ocupado()
    {
        var f = await DuasLocadoras();

        // A locadora A aluga o Onix de amanhã a daqui a 5 dias.
        var cliente = await f.A.Api.Post("/api/clientes", ClientePj("Empresa Alfa"));
        var condutor = (await f.A.Api.Post("/api/condutores", Condutor(cliente.Id, "Carlos", Dia(1000)))).Id;
        Assert.Equal(201, (await f.A.Api.Post("/api/alugueis", PedidoAluguel(condutor, f.Onix, saida: 1, retorno: 5, seguro: "Nenhum"))).Codigo);

        var pessoa = await NovoClienteDoCatalogo();
        var escopo = $"tamanhoPagina=50&texto={f.Marca}";

        // Período que bate com o aluguel: o Onix some da lista (por padrão só mostra disponíveis).
        var ocupado = await pessoa.Get($"/api/catalogo?{escopo}&retirada={Dia(2)}&devolucao={Dia(4)}");
        Assert.DoesNotContain("Onix", Modelos(ocupado));
        Assert.Contains("Hilux", Modelos(ocupado));
        Assert.Equal(2, ocupado.Corpo.GetProperty("dias").GetInt32());

        // Pedindo também os indisponíveis, o Onix volta marcado e depois de todos os disponíveis.
        var todos = await pessoa.Get($"/api/catalogo?{escopo}&apenasDisponiveis=false&retirada={Dia(2)}&devolucao={Dia(4)}");
        var registros = todos.Corpo.GetProperty("registros").EnumerateArray().ToList();
        var onix = registros.First(x => x.GetProperty("modelo").GetString() == "Onix");
        Assert.False(onix.GetProperty("disponivel").GetBoolean());

        var primeiroIndisponivel = registros.FindIndex(x => !x.GetProperty("disponivel").GetBoolean());
        Assert.All(registros.Skip(primeiroIndisponivel), x => Assert.False(x.GetProperty("disponivel").GetBoolean()));

        // Total do período: 2 diárias x menor diária do grupo (Utilitario = 150) para o Hilux.
        var hilux = registros.First(x => x.GetProperty("modelo").GetString() == "Hilux");
        Assert.Equal(150m, hilux.GetProperty("diariaAPartirDe").GetDecimal());
        Assert.Equal(300m, hilux.GetProperty("totalDoPeriodo").GetDecimal());

        // Depois do aluguel, o Onix está livre de novo.
        var depois = await pessoa.Get($"/api/catalogo?{escopo}&retirada={Dia(6)}&devolucao={Dia(8)}");
        Assert.Contains("Onix", Modelos(depois));
    }

    [Fact]
    public async Task Sem_datas_nao_ha_disponibilidade_nem_total()
    {
        var f = await DuasLocadoras();
        var pessoa = await NovoClienteDoCatalogo();

        var r = await pessoa.Get($"/api/catalogo?texto={f.Marca}");
        var primeiro = r.Corpo.GetProperty("registros")[0];

        Assert.Equal(JsonValueKind.Null, primeiro.GetProperty("disponivel").ValueKind);
        Assert.Equal(JsonValueKind.Null, primeiro.GetProperty("totalDoPeriodo").ValueKind);
    }

    [Theory]
    [InlineData("retirada=2099-01-01")]
    [InlineData("devolucao=2099-01-01")]
    [InlineData("retirada=2099-01-05&devolucao=2099-01-01")]
    [InlineData("retirada=2001-01-01&devolucao=2001-01-05")]
    public async Task Datas_invalidas_sao_recusadas(string consulta)
    {
        var pessoa = await NovoClienteDoCatalogo();

        Assert.Equal(400, (await pessoa.Get("/api/catalogo?" + consulta)).Codigo);
    }

    // ---------------- foto e exclusão ----------------

    [Fact]
    public async Task Foto_do_catalogo_so_existe_para_carros_com_foto_e_sem_login_e_recusada()
    {
        var f = await DuasLocadoras();
        var pessoa = await NovoClienteDoCatalogo();

        Assert.Equal(200, (await f.A.Api.EnviarArquivo($"/api/automoveis/{f.Onix}/foto", Png, "foto.png", "image/png")).Codigo);

        var comFoto = await pessoa.BaixarArquivo($"/api/catalogo/{f.Onix}/foto");
        Assert.Equal(System.Net.HttpStatusCode.OK, comFoto.Status);
        Assert.Equal(Png, comFoto.Bytes);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await pessoa.BaixarArquivo($"/api/catalogo/{f.Hilux}/foto")).Status);

        var anonimo = new ClienteApi(fabrica.CreateClient());
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await anonimo.BaixarArquivo($"/api/catalogo/{f.Onix}/foto")).Status);
    }

    [Fact]
    public async Task Carro_excluido_some_do_catalogo()
    {
        var f = await DuasLocadoras();
        var pessoa = await NovoClienteDoCatalogo();

        Assert.Equal(204, (await f.A.Api.Delete($"/api/automoveis/{f.Onix}")).Codigo);

        var r = await pessoa.Get($"/api/catalogo?texto={f.Marca}&tamanhoPagina=50");

        Assert.DoesNotContain("Onix", Modelos(r));
        Assert.Equal(3, r.Corpo.GetProperty("total").GetInt32());
    }
}
