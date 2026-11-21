using System.Text.RegularExpressions;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class ConfirmacaoDeEmailIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private ClienteApi NovaApi() => new(fabrica.CreateClient());

    private static int Confirmacoes(string email) =>
        EmailsDeTeste.Recebidos(email).Count(e => e.Assunto.Contains("confirme", StringComparison.OrdinalIgnoreCase));

    private static string CodigoDoUltimoEmail(string email) =>
        Regex.Match(
            EmailsDeTeste.Recebidos(email).Last(e => e.Assunto.Contains("confirme", StringComparison.OrdinalIgnoreCase)).Corpo,
            @"codigo=([A-Za-z0-9_\-]+)").Groups[1].Value;

    private async Task<(ClienteApi Api, string Email)> Cadastrar(string rota = "/api/auth/registrar-cliente")
    {
        var api = NovaApi();
        var email = Geradores.Email("confirma");

        var r = await api.Post(rota, new { nomeCompleto = "Pessoa Nova", email, senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = true });
        Assert.Equal(200, r.Codigo);

        return (api, email);
    }

    [Theory]
    [InlineData("/api/auth/registrar")]
    [InlineData("/api/auth/registrar-cliente")]
    public async Task Cadastro_nao_entra_direto_manda_o_link_e_so_depois_de_confirmar_o_login_funciona(string rota)
    {
        var (api, email) = await Cadastrar(rota);

        // O e-mail chegou, com o link para a tela de confirmação do site.
        Assert.Equal(1, Confirmacoes(email));
        var corpo = EmailsDeTeste.Recebidos(email).Last().Corpo;
        Assert.Contains("/auth/confirmar-email?email=", corpo);
        Assert.Contains("24 horas", corpo);

        // Senha certa, e-mail ainda não confirmado: 403 com a orientação (e sem token).
        var antes = await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" });
        Assert.Equal(403, antes.Codigo);
        Assert.Contains("Confirme o seu e-mail", antes.Bruto);

        // Senha errada continua sendo "login ou senha incorretos": quem não sabe a senha não descobre o estado da conta.
        Assert.Equal(400, (await api.Post("/api/auth/autenticar", new { email, senha = "Errada123" })).Codigo);

        // Abrir o link confirma; o login passa a funcionar.
        Assert.Equal(204, (await api.Post("/api/auth/confirmar-email", new { email, codigo = CodigoDoUltimoEmail(email) })).Codigo);
        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);
    }

    [Fact]
    public async Task Abrir_o_link_duas_vezes_nao_da_erro()
    {
        var (api, email) = await Cadastrar();
        var codigo = CodigoDoUltimoEmail(email);

        Assert.Equal(204, (await api.Post("/api/auth/confirmar-email", new { email, codigo })).Codigo);
        Assert.Equal(204, (await api.Post("/api/auth/confirmar-email", new { email, codigo })).Codigo);
    }

    [Fact]
    public async Task Link_invalido_ou_de_conta_inexistente_e_recusado_sem_confirmar_nada()
    {
        var (api, email) = await Cadastrar();

        Assert.Equal(400, (await api.Post("/api/auth/confirmar-email", new { email, codigo = "codigo-que-nao-vale" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/confirmar-email", new { email, codigo = "" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/confirmar-email", new { email = "ninguem@nao-existe.com", codigo = CodigoDoUltimoEmail(email) })).Codigo);

        // Continua sem poder entrar.
        Assert.Equal(403, (await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);

        // O código de uma conta não confirma a conta de outra pessoa.
        var (_, outro) = await Cadastrar();
        Assert.Equal(400, (await api.Post("/api/auth/confirmar-email", new { email = outro, codigo = CodigoDoUltimoEmail(email) })).Codigo);
    }

    [Fact]
    public async Task Reenviar_manda_um_novo_link_so_para_quem_ainda_nao_confirmou_e_a_resposta_e_sempre_a_mesma()
    {
        var (api, email) = await Cadastrar();
        Assert.Equal(1, Confirmacoes(email));

        var reenvio = await api.Post("/api/auth/reenviar-confirmacao", new { email });
        Assert.Equal(200, reenvio.Codigo);
        Assert.Equal(2, Confirmacoes(email));

        // O link novo funciona.
        Assert.Equal(204, (await api.Post("/api/auth/confirmar-email", new { email, codigo = CodigoDoUltimoEmail(email) })).Codigo);

        // Já confirmada: nada é enviado, mas a resposta é a mesma. Conta inexistente também.
        var jaConfirmada = await api.Post("/api/auth/reenviar-confirmacao", new { email });
        var inexistente = await api.Post("/api/auth/reenviar-confirmacao", new { email = "ninguem@nao-existe.com" });

        Assert.Equal(200, jaConfirmada.Codigo);
        Assert.Equal(200, inexistente.Codigo);
        Assert.Equal(reenvio.Bruto, jaConfirmada.Bruto);
        Assert.Equal(reenvio.Bruto, inexistente.Bruto);
        Assert.Equal(2, Confirmacoes(email));
        Assert.Empty(EmailsDeTeste.Recebidos("ninguem@nao-existe.com"));
    }

    [Fact]
    public async Task Redefinir_a_senha_pelo_link_do_e_mail_tambem_confirma_o_endereco()
    {
        var (api, email) = await Cadastrar();

        Assert.Equal(200, (await api.Post("/api/auth/esqueci-senha", new { email })).Codigo);

        var corpo = EmailsDeTeste.Recebidos(email).Last(e => e.Assunto.Contains("senha", StringComparison.OrdinalIgnoreCase)).Corpo;
        var codigo = Regex.Match(corpo, @"codigo=([A-Za-z0-9_\-]+)").Groups[1].Value;

        Assert.Equal(204, (await api.Post("/api/auth/redefinir-senha",
            new { email, codigo, novaSenha = "NovaSenha1", confirmarSenha = "NovaSenha1" })).Codigo);

        // Quem recebeu e usou o link provou que o e-mail é dele: entra sem confirmar de novo.
        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email, senha = "NovaSenha1" })).Codigo);
    }

    [Fact]
    public async Task Funcionario_criado_pelo_administrador_ja_entra_sem_confirmar()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        Assert.False(string.IsNullOrEmpty(c.TokenFuncionario));
    }
}
