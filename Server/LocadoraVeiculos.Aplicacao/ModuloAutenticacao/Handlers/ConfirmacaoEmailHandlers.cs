using System.Text;
using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Handlers;

/// <summary>Monta e envia o e-mail com o link de confirmação (usado no cadastro e no reenvio).</summary>
internal static class EmailDeConfirmacao
{
    public static async Task EnviarAsync(
        UserManager<Usuario> userManager,
        IEnviadorDeEmail enviadorDeEmail,
        Usuario usuario,
        string enderecoDoSite,
        CancellationToken cancellationToken)
    {
        string token = await userManager.GenerateEmailConfirmationTokenAsync(usuario);
        string codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        string link = $"{enderecoDoSite.TrimEnd('/')}/auth/confirmar-email" +
                      $"?email={Uri.EscapeDataString(usuario.Email!)}&codigo={codigo}";

        string corpo =
            $"Olá, {usuario.FullName}.\n\n" +
            "Falta só um passo para usar a sua conta no SerraDrive: confirmar que este e-mail é seu.\n" +
            "Abra o link abaixo (ele vale por 24 horas):\n\n" +
            $"{link}\n\n" +
            "Se não foi você que criou a conta, ignore este e-mail: nada será ativado.";

        await enviadorDeEmail.EnviarAsync(usuario.Email!, "SerraDrive: confirme o seu e-mail", corpo, cancellationToken);
    }
}

public class ConfirmarEmailCommandHandler(
    UserManager<Usuario> userManager,
    ILogger<ConfirmarEmailCommandHandler> logger
) : IRequestHandler<ConfirmarEmailCommand, Result>
{
    private const string LinkInvalido = "O link é inválido ou já expirou. Peça um novo e-mail de confirmação.";

    public async Task<Result> Handle(ConfirmarEmailCommand command, CancellationToken cancellationToken)
    {
        try
        {
            Usuario? usuario = string.IsNullOrWhiteSpace(command.Email)
                ? null
                : await userManager.FindByEmailAsync(command.Email.Trim());

            if (usuario is null)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(LinkInvalido));

            // Já confirmado (clicou duas vezes no link, por exemplo): está tudo certo.
            if (usuario.EmailConfirmed)
                return Result.Ok();

            string? token = DecodificarCodigo(command.Codigo);

            if (token is null)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(LinkInvalido));

            IdentityResult resultado = await userManager.ConfirmEmailAsync(usuario, token);

            if (!resultado.Succeeded)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(LinkInvalido));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a confirmação de e-mail.");

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }

    private static string? DecodificarCodigo(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;

        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(codigo));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public class ReenviarConfirmacaoCommandHandler(
    UserManager<Usuario> userManager,
    IEnviadorDeEmail enviadorDeEmail,
    ILogger<ReenviarConfirmacaoCommandHandler> logger
) : IRequestHandler<ReenviarConfirmacaoCommand, Result>
{
    public async Task<Result> Handle(ReenviarConfirmacaoCommand command, CancellationToken cancellationToken)
    {
        try
        {
            Usuario? usuario = string.IsNullOrWhiteSpace(command.Email)
                ? null
                : await userManager.FindByEmailAsync(command.Email.Trim());

            // Conta que não existe ou que já confirmou: não envia nada, mas a resposta é a mesma.
            if (usuario is null || usuario.EmailConfirmed)
                return Result.Ok();

            await EmailDeConfirmacao.EnviarAsync(userManager, enviadorDeEmail, usuario, command.EnderecoDoSite, cancellationToken);
        }
        catch (Exception ex)
        {
            // Falha no envio não pode mudar a resposta (senão revelaria quais e-mails existem).
            logger.LogError(ex, "Não foi possível reenviar o e-mail de confirmação.");
        }

        return Result.Ok();
    }
}
