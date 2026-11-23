using System.Text.Json;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.Extensions.DependencyInjection;

namespace LocadoraDeVeiculos.Testes.Integracao;

/// <summary>Geradores de documentos válidos e únicos para os testes.</summary>
public static class Geradores
{
    private static long contador = DateTime.UtcNow.Ticks % 1_000_000_000;

    private static int Proximo() => (int)(Interlocked.Increment(ref contador) % 1_000_000_007);

    private static int Digito(int[] numeros, int[] pesos)
    {
        int soma = 0;

        for (int i = 0; i < pesos.Length; i++)
            soma += numeros[i] * pesos[i];

        int resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }

    public static string Cpf()
    {
        // 9 dígitos vindos do contador (únicos entre si) + 2 dígitos verificadores.
        string baseDigitos = (Proximo() % 1_000_000_000).ToString("D9");

        if (baseDigitos.Distinct().Count() == 1)
            baseDigitos = "1" + baseDigitos[1..];

        int[] d = new int[11];

        for (int i = 0; i < 9; i++) d[i] = baseDigitos[i] - '0';

        d[9] = Digito(d, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        d[10] = Digito(d, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

        string s = string.Concat(d);

        return $"{s[..3]}.{s[3..6]}.{s[6..9]}-{s[9..]}";
    }

    public static string Cnpj()
    {
        // 8 dígitos do contador + filial 0001 + 2 dígitos verificadores.
        string baseDigitos = (Proximo() % 100_000_000).ToString("D8") + "0001";

        int[] d = new int[14];

        for (int i = 0; i < 12; i++) d[i] = baseDigitos[i] - '0';

        d[12] = Digito(d, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        d[13] = Digito(d, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

        string s = string.Concat(d);

        return $"{s[..2]}.{s[2..5]}.{s[5..8]}/{s[8..12]}-{s[12..]}";
    }

    private static int placa = 1000;

    public static string Placa() => $"ABC{Interlocked.Increment(ref placa)}";

    public static string Email(string prefixo) => $"{prefixo}.{Guid.NewGuid():N}@teste.local";
}

/// <summary>Uma empresa com cadastros básicos prontos, para os testes montarem aluguéis.</summary>
public class Cenario
{
    public required ClienteApi Api { get; init; }
    public required string TokenEmpresa { get; init; }
    public string? TokenFuncionario { get; private set; }
    public Guid FuncionarioId { get; private set; }

    public ApiFactory Fabrica { get; private set; } = null!;
    public Guid EmpresaId => EmpresaDoToken(TokenEmpresa);

    public Guid GrupoId { get; private set; }
    public Guid GrupoSemPlanoId { get; private set; }
    public Guid PlanoId { get; private set; }
    public Guid ClientePjId { get; private set; }
    public Guid ClientePfId { get; private set; }
    public Guid CondutorId { get; private set; }
    public Guid CondutorPfId { get; private set; }
    public Guid CondutorCnhCurtaId { get; private set; }
    public Guid TaxaDiariaId { get; private set; }
    public Guid TaxaFixaId { get; private set; }

    /// <summary>Lê o EmpresaId do token (as contas de teste não têm outra forma de dizer a qual empresa pertencem).</summary>
    public static Guid EmpresaDoToken(string token)
    {
        string payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var json = JsonDocument.Parse(Convert.FromBase64String(payload));

        return json.RootElement.GetProperty("EmpresaId").GetGuid();
    }

    // O cadastro avulso de cliente saiu da API (o cliente nasce com o aluguel), então os testes que precisam de
    // um cliente "já existente" o criam direto no banco, como se ele tivesse alugado antes.
    public static async Task<Guid> SemearClientePj(ApiFactory fabrica, string token, string nome)
    {
        var cliente = Cliente.NovaPessoaJuridica(
            EmpresaDoToken(token), nome, "contato@empresa.com", "(47) 3333-4444", Geradores.Cnpj(),
            "SC", "Rio do Sul", "Centro", "Rua XV", "10");

        return await Salvar(fabrica, cliente);
    }

    public static async Task<Guid> SemearClientePf(ApiFactory fabrica, string token, string nome, Guid? pessoaJuridicaId = null)
    {
        var cliente = Cliente.NovaPessoaFisica(
            EmpresaDoToken(token), nome, "pessoa@ex.com", "(47) 99999-1234", Geradores.Cpf(), "1234567", "12345678901",
            "SC", "Rio do Sul", "Centro", "Rua XV", "10", pessoaJuridicaId);

        return await Salvar(fabrica, cliente);
    }

    private static async Task<Guid> Salvar(ApiFactory fabrica, Cliente cliente)
    {
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Add(cliente);
        await db.SaveChangesAsync();

        return cliente.Id;
    }

    public Task<Guid> NovoClientePj(string nome) => SemearClientePj(Fabrica, TokenEmpresa, nome);

    public Task<Guid> NovoClientePf(string nome, Guid? pessoaJuridicaId = null) =>
        SemearClientePf(Fabrica, TokenEmpresa, nome, pessoaJuridicaId);

    public static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    public static string Dia(int deslocamento) => Hoje.AddDays(deslocamento).ToString("yyyy-MM-dd");

    /// <summary>Abre o link do último e-mail de confirmação que o endereço recebeu.</summary>
    public static async Task ConfirmarEmail(ClienteApi api, string email)
    {
        var corpo = EmailsDeTeste.Recebidos(email).Last(e => e.Assunto.Contains("confirme", StringComparison.OrdinalIgnoreCase)).Corpo;
        var codigo = System.Text.RegularExpressions.Regex.Match(corpo, @"codigo=([A-Za-z0-9_\-]+)").Groups[1].Value;

        Assert.Equal(204, (await api.Post("/api/auth/confirmar-email", new { email, codigo })).Codigo);
    }

    /// <summary>Cadastra a conta, confirma o e-mail como a pessoa faria e entra: devolve o token.</summary>
    public static async Task<string> CriarContaEEntrar(ClienteApi api, string rota, string nome, string email, string senha = "Senha123")
    {
        api.UsarToken(null);

        var r = await api.Post(rota, new { nomeCompleto = nome, email, senha, confirmarSenha = senha, aceitouTermos = true });
        Assert.Equal(200, r.Codigo);

        await ConfirmarEmail(api, email);

        var login = await api.Post("/api/auth/autenticar", new { email, senha });
        Assert.Equal(200, login.Codigo);

        return login.Texto("chave");
    }

    public static Task<string> RegistrarEmpresa(ClienteApi api, string nome = "Empresa Teste") =>
        CriarContaEEntrar(api, "/api/auth/registrar", nome, Geradores.Email("empresa"));

    public static async Task<Cenario> Criar(ApiFactory fabrica, bool comFuncionario = false)
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var token = await RegistrarEmpresa(api);

        api.UsarToken(token);

        var c = new Cenario { Api = api, TokenEmpresa = token, Fabrica = fabrica };

        await c.Preparar(fabrica, comFuncionario);

        return c;
    }

    private async Task Preparar(ApiFactory fabrica, bool comFuncionario)
    {
        var r = await Api.Put("/api/configuracoes", new
        {
            precoGasolina = 6m,
            precoGas = 4m,
            precoDiesel = 5.5m,
            precoAlcool = 4m
        });
        Assert.Equal(200, r.Codigo);

        GrupoId = (await Api.Post("/api/grupos-veiculos", new { nome = "Utilitario" })).Id;
        GrupoSemPlanoId = (await Api.Post("/api/grupos-veiculos", new { nome = "Sem plano" })).Id;

        PlanoId = (await Api.Post("/api/planos-cobranca", new
        {
            grupoVeiculosId = GrupoId,
            diario = new { precoDiaria = 150m, precoPorKm = 1.5m },
            controlado = new { kmDisponiveis = 100, precoDiaria = 200m, precoPorKmExtrapolado = 2.5m },
            livre = new { precoDiaria = 300m }
        })).Id;

        ClientePjId = await NovoClientePj("Transportes Alfa");
        ClientePfId = await NovoClientePf("Maria Silva");

        CondutorId = (await Api.Post("/api/condutores", Condutor(ClientePjId, "Carlos Motorista", Dia(1000)))).Id;
        CondutorPfId = (await Api.Post("/api/condutores", Condutor(ClientePfId, "Maria Silva", Dia(1000)))).Id;
        CondutorCnhCurtaId = (await Api.Post("/api/condutores", Condutor(ClientePjId, "Cnh Curta", Dia(5)))).Id;

        TaxaDiariaId = (await Api.Post("/api/taxas-servicos", new { nome = "GPS", preco = 20m, tipoCobranca = "Diario" })).Id;
        TaxaFixaId = (await Api.Post("/api/taxas-servicos", new { nome = "Cadeirinha", preco = 50m, tipoCobranca = "Fixo" })).Id;

        if (comFuncionario)
        {
            var email = Geradores.Email("func");

            var f = await Api.Post("/api/funcionarios", new
            {
                nomeCompleto = "Func Teste",
                cpf = Geradores.Cpf(),
                email,
                senha = "Senha123",
                confirmarSenha = "Senha123",
                salario = 2000m,
                admissaoEmUtc = DateTime.UtcNow
            });
            Assert.Equal(201, f.Codigo);
            FuncionarioId = f.Id;

            var outro = new ClienteApi(fabrica.CreateClient());
            var login = await outro.Post("/api/auth/autenticar", new { email, senha = "Senha123" });
            Assert.Equal(200, login.Codigo);
            TokenFuncionario = login.Texto("chave");
        }
    }

    // ---------------- construtores de corpos de requisição ----------------

    public static object ClientePj(string nome) => new
    {
        tipo = "PessoaJuridica",
        nome,
        email = "contato@empresa.com",
        telefone = "(47) 3333-4444",
        cnpj = Geradores.Cnpj(),
        estado = "SC",
        cidade = "Rio do Sul",
        bairro = "Centro",
        rua = "Rua XV",
        numero = "10"
    };

    public static object ClientePf(string nome, Guid? pessoaJuridicaId = null) => new
    {
        tipo = "PessoaFisica",
        nome,
        email = "pessoa@ex.com",
        telefone = "(47) 99999-1234",
        cpf = Geradores.Cpf(),
        rg = "1234567",
        cnh = "12345678901",
        estado = "SC",
        cidade = "Rio do Sul",
        bairro = "Centro",
        rua = "Rua XV",
        numero = "10",
        pessoaJuridicaId
    };

    public static object Condutor(Guid clienteId, string nome, string validadeCnh) => new
    {
        clienteId,
        nome,
        email = "condutor@ex.com",
        telefone = "(47) 98888-7777",
        cpf = Geradores.Cpf(),
        cnh = "98765432100",
        validadeCnh
    };

    public static object Automovel(Guid grupoId, string combustivel = "Diesel", int litros = 60, string? placa = null, decimal seguroDiaria = 30m) => new
    {
        placa = placa ?? Geradores.Placa(),
        marca = "Fiat",
        modelo = "Toro",
        cor = "Branco",
        ano = 2023,
        tipoCombustivel = combustivel,
        capacidadeCombustivelLitros = litros,
        grupoVeiculosId = grupoId,
        seguroDiaria
    };

    public async Task<Guid> NovoAutomovel(string combustivel = "Diesel", int litros = 60, Guid? grupo = null)
    {
        var r = await Api.Post("/api/automoveis", Automovel(grupo ?? GrupoId, combustivel, litros));
        Assert.Equal(201, r.Codigo);

        return r.Id;
    }

    public static object PedidoAluguel(
        Guid condutorId,
        Guid automovelId,
        string plano = "Diario",
        int saida = 0,
        int retorno = 3,
        int kmInicial = 1000,
        string seguro = "Contratado",
        Guid[]? taxas = null) => new
        {
            condutorId,
            automovelId,
            tipoPlano = plano,
            dataSaida = Dia(saida),
            dataRetornoPrevista = Dia(retorno),
            kmInicial,
            tipoSeguro = seguro,
            taxasIds = taxas ?? Array.Empty<Guid>()
        };
}
