using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;

namespace LocadoraDeVeiculos.WebApi.Config.Seguranca;

public static class SegurancaConfig
{
    public const string PoliticaDeLogin = "login";
    public const string PoliticaDeRecuperacao = "recuperacao";
    public const string PoliticaDeCadastro = "cadastro";
    public const string PoliticaDeMensagens = "mensagens";

    /// <summary>
    /// Limita as tentativas por endereço (IP) nas rotas que atacantes e robôs mais martelam:
    /// login, cadastro e recuperação de senha. Os limites podem ser ajustados pela configuração.
    /// </summary>
    public static IServiceCollection AddLimiteDeTentativas(this IServiceCollection services, IConfiguration configuracao)
    {
        int loginPorMinuto = Ler(configuracao, "LIMITE_LOGIN_POR_MINUTO", 10);
        int recuperacaoPorQuinzeMinutos = Ler(configuracao, "LIMITE_RECUPERACAO_POR_15_MINUTOS", 5);
        int cadastroPorHora = Ler(configuracao, "LIMITE_CADASTRO_POR_HORA", 10);
        int mensagensPorMinuto = Ler(configuracao, "LIMITE_MENSAGENS_POR_MINUTO", 20);

        services.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Mesmo formato dos outros erros da API: o primeiro texto é o título, o resto é o detalhe.
            opcoes.OnRejected = async (contexto, cancellationToken) =>
            {
                contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await contexto.HttpContext.Response.WriteAsJsonAsync(
                    new[] { "Muitas tentativas", "Muitas tentativas em pouco tempo. Aguarde um pouco e tente de novo." },
                    cancellationToken);
            };

            opcoes.AddPolicy(PoliticaDeLogin, contexto => Janela(contexto, loginPorMinuto, TimeSpan.FromMinutes(1)));
            opcoes.AddPolicy(PoliticaDeRecuperacao, contexto => Janela(contexto, recuperacaoPorQuinzeMinutos, TimeSpan.FromMinutes(15)));
            opcoes.AddPolicy(PoliticaDeCadastro, contexto => Janela(contexto, cadastroPorHora, TimeSpan.FromHours(1)));
            opcoes.AddPolicy(PoliticaDeMensagens, contexto => Janela(contexto, mensagensPorMinuto, TimeSpan.FromMinutes(1), porUsuario: true));
        });

        return services;
    }

    /// <summary>
    /// Guarda as chaves de proteção de dados em uma pasta (DATA_PROTECTION_PATH). Sem isso as chaves ficam só na memória
    /// e, a cada reinício, os links de "esqueci minha senha" já enviados deixam de valer.
    /// </summary>
    public static IServiceCollection AddChavesPersistentes(this IServiceCollection services, IConfiguration configuracao)
    {
        string? pasta = configuracao["DATA_PROTECTION_PATH"];

        if (!string.IsNullOrWhiteSpace(pasta))
        {
            Directory.CreateDirectory(pasta);

            services.AddDataProtection()
                .SetApplicationName("SerraDrive")
                .PersistKeysToFileSystem(new DirectoryInfo(pasta));
        }

        return services;
    }

    private static RateLimitPartition<string> Janela(HttpContext contexto, int limite, TimeSpan janela, bool porUsuario = false)
    {
        string chave = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        // Por usuário logado quando der (o limiter roda depois da autenticação); sem login, cai para o IP.
        if (porUsuario && contexto.User.Identity?.IsAuthenticated == true)
            chave = "usuario-" + (contexto.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                                  ?? contexto.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? chave);

        return RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limite,
            Window = janela,
            QueueLimit = 0
        });
    }

    private static int Ler(IConfiguration configuracao, string chave, int padrao) =>
        int.TryParse(configuracao[chave], out int valor) && valor > 0 ? valor : padrao;
}
