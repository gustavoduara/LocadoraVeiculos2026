using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LocadoraDeVeiculos.Testes.Integracao;

/// <summary>
/// Sobe a API inteira (controllers, MediatR, Identity/JWT) com banco em memória, sem tocar em SQL Server.
/// As chaves abaixo são valores de teste: nada aqui é segredo real.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string nomeBanco = "locadora-testes-" + Guid.NewGuid();

    static ApiFactory()
    {
        Environment.SetEnvironmentVariable("SQL_CONNECTION_STRING", "Server=ignorado;Database=ignorado");
        Environment.SetEnvironmentVariable("JWT_GENERATION_KEY", "chave-de-teste-com-mais-de-32-caracteres-123456");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE_DOMAIN", "http://localhost");
        Environment.SetEnvironmentVariable("CORS_ALLOWED_ORIGINS", "http://localhost:4200");
        Environment.SetEnvironmentVariable("LUCKYPENNY_LICENSE_KEY", "chave-de-teste");
        // Os testes fazem muitos logins e cadastros do mesmo endereço: os limites ficam altos (há testes próprios para o limite).
        Environment.SetEnvironmentVariable("LIMITE_LOGIN_POR_MINUTO", "100000");
        Environment.SetEnvironmentVariable("LIMITE_RECUPERACAO_POR_15_MINUTOS", "100000");
        Environment.SetEnvironmentVariable("LIMITE_CADASTRO_POR_HORA", "100000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" evita rodar as migrations do SQL Server (só acontecem em Development).
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(nomeBanco));

            // Os e-mails (redefinição de senha) ficam guardados em memória para os testes lerem o link.
            services.RemoveAll<IEnviadorDeEmail>();
            services.AddSingleton<EmailsDeTeste>();
            services.AddSingleton<IEnviadorDeEmail>(sp => sp.GetRequiredService<EmailsDeTeste>());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        // A API redireciona http -> https; o endereço base já é https para não haver redirecionamento.
        client.BaseAddress = new Uri("https://localhost");
    }
}


/// <summary>Enviador de e-mail dos testes: guarda as mensagens em vez de enviar.</summary>
public class EmailsDeTeste : IEnviadorDeEmail
{
    private readonly List<(string Para, string Assunto, string Corpo)> enviados = [];

    public IReadOnlyList<(string Para, string Assunto, string Corpo)> Enviados
    {
        get { lock (enviados) return enviados.ToList(); }
    }

    public Task EnviarAsync(string para, string assunto, string corpoTexto, CancellationToken cancellationToken = default)
    {
        lock (enviados) enviados.Add((para, assunto, corpoTexto));

        return Task.CompletedTask;
    }
}
