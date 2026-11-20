using System.Net;
using System.Net.Mail;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;

namespace LocadoraDeVeiculos.WebApi.Config.Email;

/// <summary>
/// Sem servidor de e-mail configurado, a mensagem (com o link) vai para o log da aplicação.
/// Serve para desenvolvimento; em produção, configure o SMTP_HOST.
/// </summary>
public class EnviadorDeEmailEmLog(ILogger<EnviadorDeEmailEmLog> logger) : IEnviadorDeEmail
{
    public Task EnviarAsync(string para, string assunto, string corpoTexto, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "E-mail NÃO enviado (SMTP não configurado). Para: {Para} | Assunto: {Assunto}\n{Corpo}", para, assunto, corpoTexto);

        return Task.CompletedTask;
    }
}

/// <summary>Envio por SMTP. Configuração: SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM e SMTP_SSL.</summary>
public class EnviadorDeEmailSmtp(IConfiguration configuracao) : IEnviadorDeEmail
{
    public async Task EnviarAsync(string para, string assunto, string corpoTexto, CancellationToken cancellationToken = default)
    {
        string host = configuracao["SMTP_HOST"]!;
        int porta = int.TryParse(configuracao["SMTP_PORT"], out int p) ? p : 587;
        string? usuario = configuracao["SMTP_USER"];
        string? senha = configuracao["SMTP_PASSWORD"];
        string remetente = configuracao["SMTP_FROM"] ?? usuario ?? "nao-responda@serradrive.local";
        bool ssl = !string.Equals(configuracao["SMTP_SSL"], "false", StringComparison.OrdinalIgnoreCase);

        using var cliente = new SmtpClient(host, porta) { EnableSsl = ssl };

        if (!string.IsNullOrWhiteSpace(usuario))
            cliente.Credentials = new NetworkCredential(usuario, senha);

        using var mensagem = new MailMessage(remetente, para, assunto, corpoTexto);

        await cliente.SendMailAsync(mensagem, cancellationToken);
    }
}

public static class EmailConfig
{
    /// <summary>Usa SMTP quando o SMTP_HOST está configurado; senão, só registra a mensagem no log.</summary>
    public static IServiceCollection AddEnvioDeEmail(this IServiceCollection services, IConfiguration configuracao)
    {
        if (!string.IsNullOrWhiteSpace(configuracao["SMTP_HOST"]))
            services.AddScoped<IEnviadorDeEmail, EnviadorDeEmailSmtp>();
        else
            services.AddScoped<IEnviadorDeEmail, EnviadorDeEmailEmLog>();

        return services;
    }
}
