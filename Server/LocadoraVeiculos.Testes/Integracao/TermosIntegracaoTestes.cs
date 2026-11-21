using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class TermosIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private ClienteApi NovaApi() => new(fabrica.CreateClient());

    private async Task<Usuario?> Buscar(string email)
    {
        using var escopo = fabrica.Services.CreateScope();

        return await escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByEmailAsync(email);
    }

    [Theory]
    [InlineData("/api/auth/registrar")]
    [InlineData("/api/auth/registrar-cliente")]
    public async Task Cadastro_sem_aceitar_os_termos_e_recusado_e_nada_e_criado(string rota)
    {
        var api = NovaApi();
        var email = Geradores.Email("termos");

        // Sem o campo, e com o campo falso: as duas formas são recusadas.
        var semCampo = await api.Post(rota, new { nomeCompleto = "Pessoa", email, senha = "Senha123", confirmarSenha = "Senha123" });
        var recusou = await api.Post(rota, new { nomeCompleto = "Pessoa", email, senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = false });

        Assert.Equal(400, semCampo.Codigo);
        Assert.Equal(400, recusou.Codigo);
        Assert.Contains("Termos de Uso", recusou.Bruto);

        // Nenhuma conta nasceu e nenhum e-mail saiu.
        Assert.Null(await Buscar(email));
        Assert.Empty(EmailsDeTeste.Recebidos(email));
    }

    [Theory]
    [InlineData("/api/auth/registrar")]
    [InlineData("/api/auth/registrar-cliente")]
    public async Task Cadastro_guarda_quando_e_qual_versao_dos_termos_foi_aceita(string rota)
    {
        var api = NovaApi();
        var email = Geradores.Email("termos");
        var antes = DateTime.UtcNow.AddMinutes(-1);

        var r = await api.Post(rota, new { nomeCompleto = "Pessoa", email, senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = true });
        Assert.Equal(200, r.Codigo);

        var usuario = await Buscar(email);

        Assert.NotNull(usuario);
        Assert.Equal(TermosDeUso.VersaoAtual, usuario.VersaoTermosAceita);
        Assert.NotNull(usuario.AceiteTermosEmUtc);
        Assert.InRange(usuario.AceiteTermosEmUtc.Value, antes, DateTime.UtcNow.AddMinutes(1));
    }
}
