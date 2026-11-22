using System.Text.RegularExpressions;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

/// <summary>Bloqueio da conta depois de várias senhas erradas seguidas (padrão: 5 tentativas).</summary>
public class BloqueioDeContaIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private ClienteApi NovaApi() => new(fabrica.CreateClient());

    private async Task<string> NovaConta()
    {
        var email = Geradores.Email("bloqueio");

        await CriarContaEEntrar(NovaApi(), "/api/auth/registrar-cliente", "Fulana Teste", email);

        return email;
    }

    private Task<Resposta> Entrar(string email, string senha) =>
        NovaApi().Post("/api/auth/autenticar", new { email, senha });

    [Fact]
    public async Task Depois_de_cinco_senhas_erradas_ate_a_senha_certa_e_recusada()
    {
        var email = await NovaConta();

        for (int i = 0; i < 5; i++)
            Assert.Equal(400, (await Entrar(email, "Errada123")).Codigo);

        var bloqueada = await Entrar(email, "Senha123");
        Assert.Equal(400, bloqueada.Codigo);

        // A resposta é a mesma de e-mail sem conta: o bloqueio também não revela que a conta existe.
        var semConta = await Entrar("ninguem@nao-existe.com", "Senha123");
        Assert.Equal(semConta.Bruto, bloqueada.Bruto);
    }

    [Fact]
    public async Task Entrar_com_sucesso_zera_a_contagem_de_erros()
    {
        var email = await NovaConta();

        for (int rodada = 0; rodada < 3; rodada++)
        {
            for (int i = 0; i < 4; i++)
                Assert.Equal(400, (await Entrar(email, "Errada123")).Codigo);

            Assert.Equal(200, (await Entrar(email, "Senha123")).Codigo);
        }
    }

    [Fact]
    public async Task Redefinir_a_senha_pelo_link_do_email_desbloqueia_a_conta()
    {
        var email = await NovaConta();

        for (int i = 0; i < 5; i++)
            await Entrar(email, "Errada123");

        Assert.Equal(400, (await Entrar(email, "Senha123")).Codigo);

        var api = NovaApi();
        Assert.Equal(200, (await api.Post("/api/auth/esqueci-senha", new { email })).Codigo);

        var corpo = EmailsDeTeste.Recebidos(email).Last(e => e.Corpo.Contains("redefinir-senha")).Corpo;
        var codigo = Regex.Match(corpo, @"codigo=([A-Za-z0-9_\-]+)").Groups[1].Value;

        Assert.Equal(204, (await api.Post("/api/auth/redefinir-senha",
            new { email, codigo, novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);

        Assert.Equal(200, (await Entrar(email, "Nova1234")).Codigo);
    }
}
