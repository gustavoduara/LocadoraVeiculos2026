using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class SenhaETipoDeAcessoIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private EmailsDeTeste Emails => fabrica.Services.GetRequiredService<EmailsDeTeste>();

    private ClienteApi NovaApi() => new(fabrica.CreateClient());

    private async Task<string> CriarConta(string rota, string senha = "Senha123")
    {
        var email = Geradores.Email("conta");

        var api = NovaApi();

        var r = await api.Post(rota, new { nomeCompleto = "Fulana de Tal", email, senha, confirmarSenha = senha, aceitouTermos = true });
        Assert.Equal(200, r.Codigo);

        await ConfirmarEmail(api, email);

        return email;
    }

    private static string CodigoDoLink(string corpo) =>
        Regex.Match(corpo, @"codigo=([A-Za-z0-9_\-]+)").Groups[1].Value;

    private async Task<string> PedirCodigo(string email)
    {
        int antes = Emails.Enviados.Count(e => e.Para == email);

        var r = await NovaApi().Post("/api/auth/esqueci-senha", new { email });
        Assert.Equal(200, r.Codigo);

        var enviados = Emails.Enviados.Where(e => e.Para == email).ToList();
        Assert.Equal(antes + 1, enviados.Count);

        return CodigoDoLink(enviados.Last().Corpo);
    }

    // ---------------- entrar pelo lado certo (Cliente ou Locadora) ----------------

    [Fact]
    public async Task Entrar_pelo_lado_errado_e_recusado_com_mensagem_clara()
    {
        var cliente = await CriarConta("/api/auth/registrar-cliente");
        var locadora = await CriarConta("/api/auth/registrar");
        var api = NovaApi();

        var clientePelaLocadora = await api.Post("/api/auth/autenticar", new { email = cliente, senha = "Senha123", tipo = "Locadora" });
        Assert.Equal(400, clientePelaLocadora.Codigo);
        Assert.Contains("conta é de cliente", clientePelaLocadora.Bruto);

        var locadoraPeloCliente = await api.Post("/api/auth/autenticar", new { email = locadora, senha = "Senha123", tipo = "Cliente" });
        Assert.Equal(400, locadoraPeloCliente.Codigo);
        Assert.Contains("conta é de locadora", locadoraPeloCliente.Bruto);

        // Pelo lado certo (e sem informar o lado, como antes) entra.
        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email = cliente, senha = "Senha123", tipo = "Cliente" })).Codigo);
        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email = locadora, senha = "Senha123", tipo = "Locadora" })).Codigo);
        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email = locadora, senha = "Senha123" })).Codigo);
    }

    [Fact]
    public async Task Funcionario_entra_pelo_lado_da_locadora_e_nao_pelo_do_cliente()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var emailFuncionario = (await c.Api.Get($"/api/funcionarios/{c.FuncionarioId}")).Texto("email");
        var api = NovaApi();

        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email = emailFuncionario, senha = "Senha123", tipo = "Locadora" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/autenticar", new { email = emailFuncionario, senha = "Senha123", tipo = "Cliente" })).Codigo);
    }

    [Fact]
    public async Task Senha_errada_nao_revela_o_tipo_da_conta()
    {
        var cliente = await CriarConta("/api/auth/registrar-cliente");

        var r = await NovaApi().Post("/api/auth/autenticar", new { email = cliente, senha = "Errada123", tipo = "Locadora" });

        Assert.Equal(400, r.Codigo);
        Assert.DoesNotContain("cliente", r.Bruto, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------- esqueci minha senha ----------------

    [Fact]
    public async Task Esqueci_senha_responde_igual_exista_ou_nao_a_conta_e_so_envia_para_quem_existe()
    {
        var email = await CriarConta("/api/auth/registrar-cliente");
        int antes = Emails.Enviados.Count;

        var existe = await NovaApi().Post("/api/auth/esqueci-senha", new { email });
        var naoExiste = await NovaApi().Post("/api/auth/esqueci-senha", new { email = "ninguem@nao-existe.com" });

        Assert.Equal(200, existe.Codigo);
        Assert.Equal(200, naoExiste.Codigo);
        Assert.Equal(existe.Texto("mensagem"), naoExiste.Texto("mensagem"));

        Assert.Equal(antes + 1, Emails.Enviados.Count);

        var enviado = Emails.Enviados.Last();
        Assert.Equal(email, enviado.Para);
        Assert.Contains("/auth/redefinir-senha?email=", enviado.Corpo);
        Assert.Contains("1 hora", enviado.Corpo);
    }

    [Fact]
    public async Task Link_do_e_mail_redefine_a_senha_e_so_pode_ser_usado_uma_vez()
    {
        var email = await CriarConta("/api/auth/registrar-cliente");
        var codigo = await PedirCodigo(email);
        var api = NovaApi();

        var redefinir = await api.Post("/api/auth/redefinir-senha", new { email, codigo, novaSenha = "Nova1234", confirmarSenha = "Nova1234" });
        Assert.Equal(204, redefinir.Codigo);

        Assert.Equal(200, (await api.Post("/api/auth/autenticar", new { email, senha = "Nova1234" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);

        // Segunda vez com o mesmo link: recusado.
        var denovo = await api.Post("/api/auth/redefinir-senha", new { email, codigo, novaSenha = "Outra1234", confirmarSenha = "Outra1234" });
        Assert.Equal(400, denovo.Codigo);
        Assert.Contains("inválido ou já expirou", denovo.Bruto);
    }

    [Fact]
    public async Task Redefinicao_recusa_codigo_invalido_conta_inexistente_senha_fraca_e_confirmacao_diferente()
    {
        var email = await CriarConta("/api/auth/registrar-cliente");
        var codigo = await PedirCodigo(email);
        var api = NovaApi();

        Assert.Equal(400, (await api.Post("/api/auth/redefinir-senha", new { email, codigo = "lixo!!", novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/redefinir-senha", new { email, codigo = "YWJj", novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);
        Assert.Equal(400, (await api.Post("/api/auth/redefinir-senha", new { email = "ninguem@nao-existe.com", codigo, novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);

        var diferente = await api.Post("/api/auth/redefinir-senha", new { email, codigo, novaSenha = "Nova1234", confirmarSenha = "Outra1234" });
        Assert.Equal(400, diferente.Codigo);
        Assert.Contains("confirmação", diferente.Bruto);

        var fraca = await api.Post("/api/auth/redefinir-senha", new { email, codigo, novaSenha = "semmaiuscula1", confirmarSenha = "semmaiuscula1" });
        Assert.Equal(400, fraca.Codigo);
        Assert.Contains("letra maiúscula", fraca.Bruto);

        // O código ainda vale depois das tentativas que falharam por senha.
        Assert.Equal(204, (await api.Post("/api/auth/redefinir-senha", new { email, codigo, novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);
    }

    [Fact]
    public async Task Redefinir_a_senha_derruba_as_sessoes_abertas()
    {
        var email = await CriarConta("/api/auth/registrar-cliente");
        var sessao = NovaApi();
        var login = await sessao.Post("/api/auth/autenticar", new { email, senha = "Senha123" });
        sessao.UsarToken(login.Texto("chave"));
        Assert.Equal(200, (await sessao.Get("/api/catalogo")).Codigo);

        var codigo = await PedirCodigo(email);
        Assert.Equal(204, (await NovaApi().Post("/api/auth/redefinir-senha", new { email, codigo, novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);

        Assert.Equal(401, (await sessao.Get("/api/catalogo")).Codigo);
    }

    [Fact]
    public async Task Funcionario_desativado_nao_recebe_o_e_mail_de_redefinicao()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var email = (await c.Api.Get($"/api/funcionarios/{c.FuncionarioId}")).Texto("email");

        await c.Api.Patch($"/api/funcionarios/{c.FuncionarioId}/status", new { ativo = false });
        int antes = Emails.Enviados.Count;

        Assert.Equal(200, (await NovaApi().Post("/api/auth/esqueci-senha", new { email })).Codigo);
        Assert.Equal(antes, Emails.Enviados.Count);
    }

    // ---------------- alterar a própria senha ----------------

    [Fact]
    public async Task Alterar_senha_exige_login_senha_atual_certa_e_nova_diferente()
    {
        var email = await CriarConta("/api/auth/registrar-cliente");
        var api = NovaApi();

        Assert.Equal(401, (await api.Post("/api/auth/alterar-senha", new { senhaAtual = "Senha123", novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);

        var login = await api.Post("/api/auth/autenticar", new { email, senha = "Senha123" });
        api.UsarToken(login.Texto("chave"));

        var errada = await api.Post("/api/auth/alterar-senha", new { senhaAtual = "Errada123", novaSenha = "Nova1234", confirmarSenha = "Nova1234" });
        Assert.Equal(400, errada.Codigo);
        Assert.Contains("senha atual está incorreta", errada.Bruto);

        var igual = await api.Post("/api/auth/alterar-senha", new { senhaAtual = "Senha123", novaSenha = "Senha123", confirmarSenha = "Senha123" });
        Assert.Equal(400, igual.Codigo);
        Assert.Contains("diferente da atual", igual.Bruto);

        Assert.Equal(400, (await api.Post("/api/auth/alterar-senha", new { senhaAtual = "Senha123", novaSenha = "Nova1234", confirmarSenha = "Outra1234" })).Codigo);

        var fraca = await api.Post("/api/auth/alterar-senha", new { senhaAtual = "Senha123", novaSenha = "semmaiuscula1", confirmarSenha = "semmaiuscula1" });
        Assert.Equal(400, fraca.Codigo);
        Assert.Contains("letra maiúscula", fraca.Bruto);

        Assert.Equal(204, (await api.Post("/api/auth/alterar-senha", new { senhaAtual = "Senha123", novaSenha = "Nova1234", confirmarSenha = "Nova1234" })).Codigo);

        // A sessão atual continua valendo, e a senha nova já é a que entra.
        Assert.Equal(200, (await api.Get("/api/catalogo")).Codigo);
        Assert.Equal(200, (await NovaApi().Post("/api/auth/autenticar", new { email, senha = "Nova1234" })).Codigo);
        Assert.Equal(400, (await NovaApi().Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);
    }

    [Fact]
    public async Task Funcionario_tambem_troca_a_propria_senha()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        c.Api.UsarToken(c.TokenFuncionario);

        var r = await c.Api.Post("/api/auth/alterar-senha", new { senhaAtual = "Senha123", novaSenha = "Nova1234", confirmarSenha = "Nova1234" });

        Assert.Equal(204, r.Codigo);
    }
}
