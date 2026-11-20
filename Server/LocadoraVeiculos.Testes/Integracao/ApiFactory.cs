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
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        // A API redireciona http -> https; o endereço base já é https para não haver redirecionamento.
        client.BaseAddress = new Uri("https://localhost");
    }
}
