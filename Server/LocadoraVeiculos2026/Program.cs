using LocadoraDeVeiculos.Aplicacao;
using LocadoraDeVeiculos.Infraestrutura.Jwt;
using LocadoraDeVeiculos.Infraestrutura.Orm;
using LocadoraDeVeiculos.WebApi.Config.Email;
using LocadoraDeVeiculos.WebApi.Config.Http;
using LocadoraDeVeiculos.WebApi.Config.Identity;
using LocadoraDeVeiculos.WebApi.Config.Orm;
using LocadoraDeVeiculos.WebApi.Config.Seguranca;
using LocadoraDeVeiculos.WebApi.Config.Swagger;
using System.Text.Json.Serialization;
namespace LocadoraDeVeiculos.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddCamadaInfraestruturaOrm(builder.Configuration)
            .AddCamadaInfraestruturaJwt();


        builder.Services.AddCamadaAplicacao(builder.Configuration);

        builder.Services.AddSwaggerConfig();
        builder.Services.AddIdentityProviderConfig(builder.Configuration);
        builder.Services.AddEnvioDeEmail(builder.Configuration);
        builder.Services.AddLimiteDeTentativas(builder.Configuration);
        builder.Services.AddChavesPersistentes(builder.Configuration);

        // Executa de hora em hora as exclusões de locadora agendadas (os testes desligam e chamam o serviço direto).
        if (!string.Equals(builder.Configuration["EXCLUSOES_AGENDADAS_DESLIGADAS"], "true", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddHostedService<Config.Conta.ExclusoesAgendadasHostedService>();


        builder.Services.ConfigureOptions<CorsConfig>() .AddCors();

        builder.Services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        var app = builder.Build();

        // Em produção as migrations só rodam quando pedido (APLICAR_MIGRACOES=true), ex.: no primeiro deploy.
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("APLICAR_MIGRACOES"))
            app.AplicarMigracoesOrm();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Publicado, o front Angular (pasta wwwroot) é servido pela própria API: uma só origem,
        // sem CORS e com o cookie de renovação da sessão no mesmo endereço.
        var servirFront = File.Exists(Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html"));

        if (servirFront)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        // Roteamento explícito DEPOIS dos arquivos estáticos: com o roteamento implícito (no começo do pipeline)
        // a rota de fallback já seria escolhida e o index.html engoliria os .js e .css.
        app.UseRouting();

        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Rotas do Angular (ex.: /alugueis) voltam para o index.html; rotas /api desconhecidas continuam 404.
        if (servirFront)
            app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");

        app.Run();
    }
}