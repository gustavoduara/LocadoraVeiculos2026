using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Infraestrutura.Jwt;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace LocadoraDeVeiculos.WebApi.Config.Identity;

public static class IdentityConfig
{
    private static int LerInteiro(IConfiguration configuracao, string chave, int padrao) =>
        int.TryParse(configuracao[chave], out int valor) && valor > 0 ? valor : padrao;

    public static void AddIdentityProviderConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantProvider, IdentityTenantProvider>();


        services.AddIdentity<Usuario, Cargo>(options =>
        {
            options.User.RequireUniqueEmail = true;

            // Bloqueio por conta: depois de N senhas erradas seguidas a conta fica bloqueada por alguns minutos
            // (vale também para a senha certa, para não dar para adivinhar por tentativa). Complementa o limite por IP.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = LerInteiro(configuration, "LIMITE_TENTATIVAS_POR_CONTA", 5);
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(LerInteiro(configuration, "BLOQUEIO_DA_CONTA_EM_MINUTOS", 15));
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.Tokens.EmailConfirmationTokenProvider = ConfirmacaoDeEmailTokenProviderOptions.NomeDoProvedor;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddTokenProvider<ConfirmacaoDeEmailTokenProvider>(ConfirmacaoDeEmailTokenProviderOptions.NomeDoProvedor);

        // O link de "esqueci minha senha" vale por 1 hora.
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(1));

        services.AddJwtAuthentication(configuration);
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var chaveAssinaturaJwt = configuration["JWT_GENERATION_KEY"];

        if (chaveAssinaturaJwt is null)
            throw new ArgumentException("Não foi possível obter a chave de assinatura de tokens.");

        var chaveEmBytes = Encoding.ASCII.GetBytes(chaveAssinaturaJwt);

        var audienciaValida = configuration["JWT_AUDIENCE_DOMAIN"];

        if (audienciaValida is null)
            throw new ArgumentException("Não foi possível obter o domínio da audiência dos tokens.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = "LocadoraDeVeiculos",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(chaveEmBytes),
                ValidAudience = audienciaValida,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true
            };

            options.Events = new JwtBearerEvents
            {
                // O WebSocket do chat não leva cabeçalho Authorization: o token vem em ?access_token=, só nas rotas /hubs.
                OnMessageReceived = context =>
                {
                    string? token = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        context.Token = token;

                    return Task.CompletedTask;
                },

                OnTokenValidated = async context =>
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();

                    var userId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (!Guid.TryParse(userId, out var uid))
                    {
                        context.HttpContext.Items["AuthErrorCode"] = "InvalidUserId";
                        context.Fail("ID de Usuário inválido.");
                        return;
                    }

                    var user = await userManager.FindByIdAsync(uid.ToString());

                    if (user is null)
                    {
                        context.HttpContext.Items["AuthErrorCode"] = "UserNotFound";
                        context.Fail("Usuário não encontrado.");
                        return;
                    }

                    var verClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                    if (!Guid.TryParse(verClaim, out var tokenVer) || !tokenVer.Equals(user.AccessTokenVersionId))
                    {
                        context.HttpContext.Items["AuthErrorCode"] = "TokenRevoked";
                        context.Fail("O token de acesso do usuário foi revogado.");
                        return;
                    }
                },

                OnChallenge = ctx =>
                {
                    ctx.HandleResponse();

                    if (ctx.Response.HasStarted)
                        return Task.CompletedTask;

                    var http = ctx.HttpContext;
                    var errorCode = http.Items["AuthErrorCode"] as string ?? "Unauthorized";

                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    http.Response.ContentType = "application/json";

                    var payload = JsonSerializer.Serialize(new
                    {
                        status = 401,
                        error = errorCode,
                        message = ctx.AuthenticateFailure?.Message ?? "Não foi possível autenticar o usuário."
                    });

                    return http.Response.WriteAsync(payload);
                },

                OnForbidden = ctx =>
                {
                    if (ctx.Response.HasStarted)
                        return Task.CompletedTask;

                    var http = ctx.HttpContext;

                    http.Response.StatusCode = StatusCodes.Status403Forbidden;
                    http.Response.ContentType = "application/json";

                    var payload = JsonSerializer.Serialize(new
                    {
                        status = 403,
                        error = "Forbidden",
                        message = "O usuário autenticado não tem permissão para acessar este recurso."
                    });

                    return http.Response.WriteAsync(payload);
                }
            };
        });
    }
}