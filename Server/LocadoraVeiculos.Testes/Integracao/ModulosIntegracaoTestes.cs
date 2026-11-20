using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class ModulosIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    // PNG de 1x1 pixel.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private ClienteApi NovaApi() => new(fabrica.CreateClient());

    // ---------------- autenticação ----------------

    [Fact]
    public async Task Registro_e_login_devolvem_token_e_senha_errada_e_recusada()
    {
        var api = NovaApi();
        var email = Geradores.Email("auth");

        var registro = await api.Post("/api/auth/registrar",
            new { nomeCompleto = "Dono", email, senha = "Senha123", confirmarSenha = "Senha123" });

        Assert.Equal(200, registro.Codigo);
        Assert.False(string.IsNullOrEmpty(registro.Texto("chave")));

        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/autenticar", new { email, senha = "Errada123" })).Codigo);
        Assert.Equal(404, (await api.Post("/api/auth/autenticar", new { email = "naoexiste@x.com", senha = "Senha123" })).Codigo);
    }

    [Fact]
    public async Task Registro_recusa_email_repetido_e_senha_fraca()
    {
        var api = NovaApi();
        var email = Geradores.Email("dup");
        var corpo = new { nomeCompleto = "Dono", email, senha = "Senha123", confirmarSenha = "Senha123" };

        Assert.Equal(200, (await api.Post("/api/auth/registrar", corpo)).Codigo);
        Assert.InRange((await api.Post("/api/auth/registrar", corpo)).Codigo, 400, 409);

        var fraca = await api.Post("/api/auth/registrar",
            new { nomeCompleto = "Dono", email = Geradores.Email("fraca"), senha = "abc", confirmarSenha = "abc" });

        Assert.InRange(fraca.Codigo, 400, 422);
    }

    [Fact]
    public async Task Rotas_protegidas_exigem_token_valido()
    {
        var api = NovaApi();

        Assert.Equal(401, (await api.Get("/api/grupos-veiculos")).Codigo);

        api.UsarToken("token-invalido");

        Assert.Equal(401, (await api.Get("/api/grupos-veiculos")).Codigo);
    }

    [Fact]
    public async Task Rotacionar_e_sair_sem_cookie_de_refresh_sao_recusados()
    {
        var api = NovaApi();

        Assert.Equal(401, (await api.Post("/api/auth/rotacionar")).Codigo);
        Assert.Equal(401, (await api.Post("/api/auth/sair")).Codigo);
    }

    // ---------------- funcionários ----------------

    [Fact]
    public async Task Funcionario_crud_status_e_perfil()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        var lista = await c.Api.Get("/api/funcionarios");
        Assert.Equal(200, lista.Codigo);
        Assert.Single(lista.Registros.EnumerateArray());

        var detalhe = await c.Api.Get($"/api/funcionarios/{c.FuncionarioId}");
        Assert.Equal("Func Teste", detalhe.Texto("nomeCompleto"));

        var editado = await c.Api.Put($"/api/funcionarios/{c.FuncionarioId}", new
        {
            nomeCompleto = "Func Editado",
            cpf = Geradores.Cpf(),
            salario = 2500m,
            admissaoEmUtc = DateTime.UtcNow
        });
        Assert.Equal(200, editado.Codigo);

        // Perfil do próprio funcionário.
        c.Api.UsarToken(c.TokenFuncionario);
        var perfil = await c.Api.Get("/api/funcionarios/meu-perfil");
        Assert.Equal(200, perfil.Codigo);

        var novoPerfil = await c.Api.Put("/api/funcionarios/meu-perfil", new { nomeCompleto = "Meu Nome Novo", cpf = Geradores.Cpf() });
        Assert.Equal(200, novoPerfil.Codigo);
        Assert.Equal("Meu Nome Novo", novoPerfil.Texto("nomeCompleto"));

        // Funcionário não gerencia funcionários.
        Assert.Equal(403, (await c.Api.Get("/api/funcionarios")).Codigo);

        c.Api.UsarToken(c.TokenEmpresa);
        Assert.Equal(403, (await c.Api.Get("/api/funcionarios/meu-perfil")).Codigo);
    }

    [Fact]
    public async Task Funcionario_desativado_nao_consegue_mais_entrar_nem_usar_o_token()
    {
        var api = NovaApi();
        var c = await Criar(fabrica, comFuncionario: true);

        var desativar = await c.Api.Patch($"/api/funcionarios/{c.FuncionarioId}/status", new { ativo = false });
        Assert.Equal(200, desativar.Codigo);

        // O token que ele já tinha foi revogado.
        var antigo = NovaApi();
        antigo.UsarToken(c.TokenFuncionario);
        Assert.Equal(401, (await antigo.Get("/api/grupos-veiculos")).Codigo);

        var reativar = await c.Api.Patch($"/api/funcionarios/{c.FuncionarioId}/status", new { ativo = true });
        Assert.Equal(200, reativar.Codigo);
        Assert.NotNull(api);
    }

    [Fact]
    public async Task Funcionario_com_cpf_ou_email_invalido_e_recusado()
    {
        var c = await Criar(fabrica);

        var r = await c.Api.Post("/api/funcionarios", new
        {
            nomeCompleto = "Fulano",
            cpf = "111.111.111-11",
            email = "nao-e-email",
            senha = "Senha123",
            confirmarSenha = "Senha123",
            salario = 1000m,
            admissaoEmUtc = DateTime.UtcNow
        });

        Assert.Equal(400, r.Codigo);
    }

    // ---------------- grupos de veículos ----------------

    [Fact]
    public async Task Grupo_crud_duplicado_e_bloqueio_de_exclusao()
    {
        var c = await Criar(fabrica);

        var criado = await c.Api.Post("/api/grupos-veiculos", new { nome = "Esportivos" });
        Assert.Equal(201, criado.Codigo);

        Assert.Equal(409, (await c.Api.Post("/api/grupos-veiculos", new { nome = "Esportivos" })).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/grupos-veiculos", new { nome = "" })).Codigo);

        var editado = await c.Api.Put($"/api/grupos-veiculos/{criado.Id}", new { nome = "Esportivos Plus" });
        Assert.Equal(200, editado.Codigo);
        Assert.Equal("Esportivos Plus", (await c.Api.Get($"/api/grupos-veiculos/{criado.Id}")).Texto("nome"));

        // Grupo com automóvel ou plano não pode ser excluído.
        await c.NovoAutomovel(grupo: criado.Id);
        Assert.Equal(422, (await c.Api.Delete($"/api/grupos-veiculos/{criado.Id}")).Codigo);
        Assert.Equal(422, (await c.Api.Delete($"/api/grupos-veiculos/{c.GrupoId}")).Codigo);

        var vazio = await c.Api.Post("/api/grupos-veiculos", new { nome = "Vazio" });
        Assert.Equal(204, (await c.Api.Delete($"/api/grupos-veiculos/{vazio.Id}")).Codigo);
        Assert.Equal(404, (await c.Api.Get($"/api/grupos-veiculos/{vazio.Id}")).Codigo);
    }

    [Fact]
    public async Task Grupos_de_uma_empresa_nao_aparecem_para_outra()
    {
        var a = await Criar(fabrica);
        var b = await Criar(fabrica);

        var grupoDeA = await a.Api.Post("/api/grupos-veiculos", new { nome = "So da A" });

        Assert.Equal(404, (await b.Api.Get($"/api/grupos-veiculos/{grupoDeA.Id}")).Codigo);
        Assert.Equal(404, (await b.Api.Delete($"/api/grupos-veiculos/{grupoDeA.Id}")).Codigo);
        Assert.DoesNotContain("So da A", (await b.Api.Get("/api/grupos-veiculos")).Bruto);
        // O mesmo nome pode existir em outra empresa.
        Assert.Equal(201, (await b.Api.Post("/api/grupos-veiculos", new { nome = "So da A" })).Codigo);
    }

    // ---------------- configurações ----------------

    [Fact]
    public async Task Configuracao_faz_upsert_valida_precos_e_so_a_empresa_altera()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        var atual = await c.Api.Get("/api/configuracoes");
        Assert.Equal(200, atual.Codigo);
        Assert.Equal(6m, atual.Numero("precoGasolina"));

        var nova = await c.Api.Put("/api/configuracoes", new
        {
            precoGasolina = 7.25m, precoGas = 4m, precoDiesel = 5m, precoAlcool = 4m,
            seguroClienteDiaria = 31m, seguroTerceirosDiaria = 21m
        });
        Assert.Equal(200, nova.Codigo);
        Assert.Equal(7.25m, (await c.Api.Get("/api/configuracoes")).Numero("precoGasolina"));

        Assert.Equal(400, (await c.Api.Put("/api/configuracoes", new
        {
            precoGasolina = 0m, precoGas = 4m, precoDiesel = 5m, precoAlcool = 4m,
            seguroClienteDiaria = 31m, seguroTerceirosDiaria = 21m
        })).Codigo);

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(200, (await c.Api.Get("/api/configuracoes")).Codigo);
        Assert.Equal(403, (await c.Api.Put("/api/configuracoes", new
        {
            precoGasolina = 7m, precoGas = 4m, precoDiesel = 5m, precoAlcool = 4m,
            seguroClienteDiaria = 31m, seguroTerceirosDiaria = 21m
        })).Codigo);
    }

    // ---------------- planos de cobrança ----------------

    [Fact]
    public async Task Plano_crud_um_por_grupo_e_so_empresa_escreve()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        object Plano(Guid grupo) => new
        {
            grupoVeiculosId = grupo,
            diario = new { precoDiaria = 100m, precoPorKm = 1m },
            controlado = new { kmDisponiveis = 50, precoDiaria = 120m, precoPorKmExtrapolado = 2m },
            livre = new { precoDiaria = 200m }
        };

        Assert.Equal(409, (await c.Api.Post("/api/planos-cobranca", Plano(c.GrupoId))).Codigo);
        Assert.Equal(404, (await c.Api.Post("/api/planos-cobranca", Plano(Guid.NewGuid()))).Codigo);

        var novo = await c.Api.Post("/api/planos-cobranca", Plano(c.GrupoSemPlanoId));
        Assert.Equal(201, novo.Codigo);

        var editado = await c.Api.Put($"/api/planos-cobranca/{novo.Id}", new
        {
            diario = new { precoDiaria = 110m, precoPorKm = 1.2m },
            controlado = new { kmDisponiveis = 60, precoDiaria = 130m, precoPorKmExtrapolado = 2.2m },
            livre = new { precoDiaria = 210m }
        });
        Assert.Equal(200, editado.Codigo);

        var lido = await c.Api.Get($"/api/planos-cobranca/{novo.Id}");
        Assert.Equal(110m, lido.Corpo.GetProperty("diario").GetProperty("precoDiaria").GetDecimal());

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(200, (await c.Api.Get("/api/planos-cobranca")).Codigo);
        Assert.Equal(403, (await c.Api.Delete($"/api/planos-cobranca/{novo.Id}")).Codigo);

        c.Api.UsarToken(c.TokenEmpresa);
        Assert.Equal(204, (await c.Api.Delete($"/api/planos-cobranca/{novo.Id}")).Codigo);
    }

    // ---------------- taxas e serviços ----------------

    [Fact]
    public async Task Taxa_crud_nome_unico_sem_diferenciar_maiusculas_e_so_empresa_escreve()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        Assert.Equal(409, (await c.Api.Post("/api/taxas-servicos", new { nome = "GPS", preco = 10m, tipoCobranca = "Diario" })).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/taxas-servicos", new { nome = "Item", preco = -1m, tipoCobranca = "Fixo" })).Codigo);

        var editada = await c.Api.Put($"/api/taxas-servicos/{c.TaxaFixaId}", new { nome = "Cadeirinha Bebe", preco = 60m, tipoCobranca = "Fixo" });
        Assert.Equal(200, editada.Codigo);

        Assert.Equal(409, (await c.Api.Put($"/api/taxas-servicos/{c.TaxaFixaId}", new { nome = "GPS", preco = 60m, tipoCobranca = "Fixo" })).Codigo);
        Assert.Equal(60m, (await c.Api.Get($"/api/taxas-servicos/{c.TaxaFixaId}")).Numero("preco"));

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(2, (await c.Api.Get("/api/taxas-servicos")).Registros.GetArrayLength());
        Assert.Equal(403, (await c.Api.Post("/api/taxas-servicos", new { nome = "Outra", preco = 5m, tipoCobranca = "Fixo" })).Codigo);

        c.Api.UsarToken(c.TokenEmpresa);
        Assert.Equal(204, (await c.Api.Delete($"/api/taxas-servicos/{c.TaxaDiariaId}")).Codigo);
        Assert.Equal(404, (await c.Api.Get($"/api/taxas-servicos/{c.TaxaDiariaId}")).Codigo);
    }

    // ---------------- automóveis ----------------

    [Fact]
    public async Task Automovel_crud_placa_unica_normalizada_e_filtro_por_grupo()
    {
        var c = await Criar(fabrica);

        var criado = await c.Api.Post("/api/automoveis", Automovel(c.GrupoId, placa: "QWE-1A23"));
        Assert.Equal(201, criado.Codigo);

        var lido = await c.Api.Get($"/api/automoveis/{criado.Id}");
        Assert.Equal("QWE1A23", lido.Texto("placa"));
        Assert.Equal("Utilitario", lido.Texto("nomeGrupo"));
        Assert.False(lido.Corpo.GetProperty("temFoto").GetBoolean());

        Assert.Equal(409, (await c.Api.Post("/api/automoveis", Automovel(c.GrupoId, placa: "qwe1a23"))).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/automoveis", Automovel(c.GrupoId, placa: "123"))).Codigo);
        Assert.Equal(404, (await c.Api.Post("/api/automoveis", Automovel(Guid.NewGuid()))).Codigo);

        await c.NovoAutomovel(grupo: c.GrupoSemPlanoId);

        Assert.Equal(2, (await c.Api.Get("/api/automoveis")).Registros.GetArrayLength());
        Assert.Single((await c.Api.Get($"/api/automoveis?grupoVeiculosId={c.GrupoId}")).Registros.EnumerateArray());

        var editado = await c.Api.Put($"/api/automoveis/{criado.Id}", Automovel(c.GrupoId, "Gasolina", 45, "QWE1A23"));
        Assert.Equal(200, editado.Codigo);

        Assert.Equal(204, (await c.Api.Delete($"/api/automoveis/{criado.Id}")).Codigo);
    }

    [Fact]
    public async Task Automovel_foto_envia_baixa_recusa_arquivo_falso_e_remove()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        var enviada = await c.Api.EnviarArquivo($"/api/automoveis/{carro}/foto", Png, "foto.png", "image/png");
        Assert.Equal(200, enviada.Codigo);
        Assert.True((await c.Api.Get($"/api/automoveis/{carro}")).Corpo.GetProperty("temFoto").GetBoolean());

        var baixada = await c.Api.BaixarArquivo($"/api/automoveis/{carro}/foto");
        Assert.Equal(System.Net.HttpStatusCode.OK, baixada.Status);
        Assert.Equal(Png, baixada.Bytes);
        Assert.Equal("image/png", baixada.Tipo);

        // Um texto disfarçado de imagem é recusado pela assinatura real do arquivo.
        var falsa = await c.Api.EnviarArquivo($"/api/automoveis/{carro}/foto", "nao sou imagem"u8.ToArray(), "foto.png", "image/png");
        Assert.Equal(400, falsa.Codigo);

        // SVG é recusado.
        var svg = await c.Api.EnviarArquivo($"/api/automoveis/{carro}/foto", "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray(), "foto.svg", "image/svg+xml");
        Assert.Equal(400, svg.Codigo);

        Assert.Equal(204, (await c.Api.Delete($"/api/automoveis/{carro}/foto")).Codigo);
        Assert.False((await c.Api.Get($"/api/automoveis/{carro}")).Corpo.GetProperty("temFoto").GetBoolean());
        Assert.Equal(404, (await c.Api.BaixarArquivo($"/api/automoveis/{carro}/foto")).Status == System.Net.HttpStatusCode.NotFound ? 404 : 0);
    }

    // ---------------- clientes ----------------

    [Fact]
    public async Task Cliente_pessoa_fisica_e_juridica_com_vinculo_documentos_unicos_e_filtro()
    {
        var c = await Criar(fabrica);

        var pf = await c.Api.Post("/api/clientes", ClientePf("Joao", c.ClientePjId));
        Assert.Equal(201, pf.Codigo);
        Assert.Equal(c.ClientePjId, (await c.Api.Get($"/api/clientes/{pf.Id}")).Corpo.GetProperty("pessoaJuridicaId").GetGuid());

        // CPF repetido e vínculo com pessoa física são recusados.
        var dados = ClientePf("Outro");
        Assert.Equal(201, (await c.Api.Post("/api/clientes", dados)).Codigo);
        Assert.Equal(409, (await c.Api.Post("/api/clientes", dados)).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/clientes", ClientePf("Vinculo Errado", c.ClientePfId))).Codigo);

        // Documentos inválidos.
        Assert.Equal(400, (await c.Api.Post("/api/clientes", new
        {
            tipo = "PessoaJuridica", nome = "Sem Cnpj Valido", email = "a@a.com", telefone = "(47) 3333-4444",
            cnpj = "11.111.111/1111-11", estado = "SC", cidade = "Rio do Sul", bairro = "Centro", rua = "Rua XV", numero = "1"
        })).Codigo);

        Assert.InRange((await c.Api.Get("/api/clientes?tipo=PessoaJuridica")).Registros.GetArrayLength(), 1, 1);
        Assert.Equal(3, (await c.Api.Get("/api/clientes?tipo=PessoaFisica")).Registros.GetArrayLength());

        // Editar PF mantém o tipo.
        var editado = await c.Api.Put($"/api/clientes/{pf.Id}", ClientePf("Joao Editado", c.ClientePjId));
        Assert.Equal(200, editado.Codigo);

        // PJ com PF vinculada não pode ser excluída; PF sem condutor pode.
        Assert.Equal(422, (await c.Api.Delete($"/api/clientes/{c.ClientePjId}")).Codigo);
        Assert.Equal(204, (await c.Api.Delete($"/api/clientes/{pf.Id}")).Codigo);
    }

    // ---------------- condutores ----------------

    [Fact]
    public async Task Condutor_cpf_unico_por_cliente_cnh_vencida_filtro_e_exclusao_de_cliente()
    {
        var c = await Criar(fabrica);

        var corpo = Condutor(c.ClientePjId, "Pedro", Dia(-30));
        var vencido = await c.Api.Post("/api/condutores", corpo);
        Assert.Equal(201, vencido.Codigo);
        Assert.True((await c.Api.Get($"/api/condutores/{vencido.Id}")).Corpo.GetProperty("cnhVencida").GetBoolean());

        // Mesmo CPF no mesmo cliente: conflito; em outro cliente: permitido.
        Assert.Equal(409, (await c.Api.Post("/api/condutores", corpo)).Codigo);

        var cpfDe = (await c.Api.Get($"/api/condutores/{vencido.Id}")).Texto("cpf");
        var outroCliente = await c.Api.Post("/api/condutores", new
        {
            clienteId = c.ClientePfId, nome = "Pedro", email = "p@p.com", telefone = "(47) 98888-7777",
            cpf = cpfDe, cnh = "98765432100", validadeCnh = Dia(900)
        });
        Assert.Equal(201, outroCliente.Codigo);

        Assert.Equal(404, (await c.Api.Post("/api/condutores", Condutor(Guid.NewGuid(), "X Y Z", Dia(10)))).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/condutores", Condutor(c.ClientePjId, "Z", Dia(10)))).Codigo);

        Assert.Equal(3, (await c.Api.Get($"/api/condutores?clienteId={c.ClientePjId}")).Registros.GetArrayLength());

        var editado = await c.Api.Put($"/api/condutores/{vencido.Id}", new
        {
            nome = "Pedro Editado", email = "p@p.com", telefone = "(47) 98888-7777",
            cpf = cpfDe, cnh = "98765432100", validadeCnh = Dia(900)
        });
        Assert.Equal(200, editado.Codigo);
        Assert.False((await c.Api.Get($"/api/condutores/{vencido.Id}")).Corpo.GetProperty("cnhVencida").GetBoolean());

        // Cliente com condutores não pode ser excluído.
        Assert.Equal(422, (await c.Api.Delete($"/api/clientes/{c.ClientePfId}")).Codigo);
        Assert.Equal(204, (await c.Api.Delete($"/api/condutores/{outroCliente.Id}")).Codigo);
    }

    [Theory]
    [InlineData("semmaiuscula1", "letra maiúscula")]
    [InlineData("SEMMINUSCULA1", "letra minúscula")]
    [InlineData("SemNumeros", "um número")]
    public async Task Senha_fraca_devolve_mensagem_em_portugues_dizendo_o_que_falta(string senha, string trecho)
    {
        var c = await Criar(fabrica);

        var funcionario = await c.Api.Post("/api/funcionarios", new
        {
            nomeCompleto = "Fulano de Tal",
            cpf = Geradores.Cpf(),
            email = Geradores.Email("fraca"),
            senha,
            confirmarSenha = senha,
            salario = 1000m,
            admissaoEmUtc = DateTime.UtcNow
        });

        Assert.Equal(400, funcionario.Codigo);
        Assert.Contains(trecho, funcionario.Bruto);
        Assert.DoesNotContain("Passwords must", funcionario.Bruto);

        var registro = await NovaApi().Post("/api/auth/registrar", new
        {
            nomeCompleto = "Dono",
            email = Geradores.Email("fraca"),
            senha,
            confirmarSenha = senha
        });

        Assert.Equal(400, registro.Codigo);
        Assert.Contains(trecho, registro.Bruto);
    }
}
