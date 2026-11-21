using Microsoft.AspNetCore.Hosting;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class SegurancaIntegracaoTestes
{
    [Fact]
    public async Task Login_e_limitado_por_endereco_e_a_resposta_avisa_para_esperar()
    {
        using var fabrica = new ApiFactory().WithWebHostBuilder(b => b.UseSetting("LIMITE_LOGIN_POR_MINUTO", "3"));
        var api = new ClienteApi(fabrica.CreateClient());

        var corpo = new { email = "ninguem@teste.local", senha = "Qualquer123" };

        // As 3 primeiras tentativas passam pelo limite (e falham por a conta não existir)...
        for (int i = 0; i < 3; i++)
            Assert.NotEqual(429, (await api.Post("/api/auth/autenticar", corpo)).Codigo);

        // ...a 4ª é barrada.
        var barrada = await api.Post("/api/auth/autenticar", corpo);

        Assert.Equal(429, barrada.Codigo);
        Assert.Contains("Aguarde um pouco", barrada.Bruto);
    }

    [Fact]
    public async Task Recuperacao_de_senha_tem_limite_proprio_e_outras_rotas_nao_sao_afetadas()
    {
        using var fabrica = new ApiFactory().WithWebHostBuilder(b => b.UseSetting("LIMITE_RECUPERACAO_POR_15_MINUTOS", "2"));
        var api = new ClienteApi(fabrica.CreateClient());

        Assert.Equal(200, (await api.Post("/api/auth/esqueci-senha", new { email = "a@teste.local" })).Codigo);
        Assert.Equal(200, (await api.Post("/api/auth/esqueci-senha", new { email = "b@teste.local" })).Codigo);
        Assert.Equal(429, (await api.Post("/api/auth/esqueci-senha", new { email = "c@teste.local" })).Codigo);

        // O login tem outro contador: continua liberado.
        Assert.NotEqual(429, (await api.Post("/api/auth/autenticar", new { email = "a@teste.local", senha = "Qualquer123" })).Codigo);
    }

    [Fact]
    public async Task Cadastro_tem_limite_por_hora()
    {
        using var fabrica = new ApiFactory().WithWebHostBuilder(b => b.UseSetting("LIMITE_CADASTRO_POR_HORA", "2"));
        var api = new ClienteApi(fabrica.CreateClient());

        object Conta() => new
        {
            nomeCompleto = "Pessoa",
            email = Geradores.Email("limite"),
            senha = "Senha123",
            confirmarSenha = "Senha123", aceitouTermos = true
        };

        Assert.Equal(200, (await api.Post("/api/auth/registrar-cliente", Conta())).Codigo);
        Assert.Equal(200, (await api.Post("/api/auth/registrar", Conta())).Codigo);
        Assert.Equal(429, (await api.Post("/api/auth/registrar-cliente", Conta())).Codigo);
    }

    [Fact]
    public async Task Chaves_de_protecao_sao_guardadas_na_pasta_configurada()
    {
        string pasta = Path.Combine(Path.GetTempPath(), "chaves-teste-" + Guid.NewGuid().ToString("N"));

        try
        {
            using var fabrica = new ApiFactory().WithWebHostBuilder(b => b.UseSetting("DATA_PROTECTION_PATH", pasta));
            var api = new ClienteApi(fabrica.CreateClient());

            var email = Geradores.Email("chave");
            await api.Post("/api/auth/registrar-cliente", new { nomeCompleto = "Pessoa", email, senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = true });

            // Gerar o código de redefinição usa as chaves de proteção, que passam a ser gravadas em arquivo.
            Assert.Equal(200, (await api.Post("/api/auth/esqueci-senha", new { email })).Codigo);

            Assert.True(Directory.Exists(pasta));
            Assert.NotEmpty(Directory.GetFiles(pasta, "key-*.xml"));
        }
        finally
        {
            if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true);
        }
    }
}
