using System.Text;
using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Infraestrutura.Jwt.Services;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloFuncionario;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Handlers;

public class SolicitarRedefinicaoSenhaCommandHandler(
    UserManager<Usuario> userManager,
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    IEnviadorDeEmail enviadorDeEmail,
    ILogger<SolicitarRedefinicaoSenhaCommandHandler> logger
) : IRequestHandler<SolicitarRedefinicaoSenhaCommand, Result>
{
    public async Task<Result> Handle(SolicitarRedefinicaoSenhaCommand command, CancellationToken cancellationToken)
    {
        try
        {
            Usuario? usuario = string.IsNullOrWhiteSpace(command.Email)
                ? null
                : await userManager.FindByEmailAsync(command.Email.Trim());

            // Conta que não existe, ou funcionário desativado: não envia nada, mas a resposta é a mesma.
            if (usuario is null || !await PodeEntrarAsync(usuario))
                return Result.Ok();

            string token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            string codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            string link = $"{command.EnderecoDoSite.TrimEnd('/')}/auth/redefinir-senha" +
                          $"?email={Uri.EscapeDataString(usuario.Email!)}&codigo={codigo}";

            string corpo =
                $"Olá, {usuario.FullName}.\n\n" +
                "Recebemos um pedido para criar uma nova senha da sua conta no SerraDrive.\n" +
                "Para continuar, abra o link abaixo (ele vale por 1 hora e só pode ser usado uma vez):\n\n" +
                $"{link}\n\n" +
                "Se não foi você, ignore este e-mail: sua senha continua a mesma.";

            await enviadorDeEmail.EnviarAsync(usuario.Email!, "SerraDrive: crie uma nova senha", corpo, cancellationToken);
        }
        catch (Exception ex)
        {
            // Falha no envio não pode mudar a resposta (senão revelaria quais e-mails existem).
            logger.LogError(ex, "Não foi possível enviar o e-mail de redefinição de senha.");
        }

        return Result.Ok();
    }

    private async Task<bool> PodeEntrarAsync(Usuario usuario)
    {
        if (!await userManager.IsInRoleAsync(usuario, CargoUsuario.Funcionario.ToString()))
            return true;

        var funcionario = await repositorioFuncionario.SelecionarPorUsuarioIdAsync(usuario.Id);

        return funcionario is { Excluido: false, Ativo: true };
    }
}

public class RedefinirSenhaCommandHandler(
    UserManager<Usuario> userManager,
    RefreshTokenProvider refreshTokenProvider,
    AppDbContext dbContext,
    ILogger<RedefinirSenhaCommandHandler> logger
) : IRequestHandler<RedefinirSenhaCommand, Result>
{
    private static readonly string LinkInvalido = "O link é inválido ou já expirou. Peça um novo.";

    public async Task<Result> Handle(RedefinirSenhaCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.NovaSenha != command.ConfirmarSenha)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A confirmação de senha falhou."));

            Usuario? usuario = string.IsNullOrWhiteSpace(command.Email)
                ? null
                : await userManager.FindByEmailAsync(command.Email.Trim());

            string? token = DecodificarCodigo(command.Codigo);

            if (usuario is null || token is null)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(LinkInvalido));

            IdentityResult resultado = await userManager.ResetPasswordAsync(usuario, token, command.NovaSenha);

            if (!resultado.Succeeded)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(ErrosDeIdentity.Traduzir(resultado.Errors)));

            // Quem tinha a senha antiga (ou o acesso de outra pessoa) perde todas as sessões abertas.
            await refreshTokenProvider.RevogarTokensUsuarioAsync(usuario.Id, "Redefinição de senha");

            usuario.AccessTokenVersionId = Guid.NewGuid();

            // Quem prova que é dono do e-mail também desbloqueia a conta (menos as já excluídas).
            if (usuario.ContaExcluidaEmUtc is null)
            {
                usuario.LockoutEnd = null;
                usuario.AccessFailedCount = 0;
            }

            // O link chegou ao e-mail da conta: quem o usou prova que o endereço é dele.
            usuario.EmailConfirmed = true;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a redefinição de senha.");

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }

    private static string? DecodificarCodigo(string codigo)
    {
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

public class AlterarSenhaCommandHandler(
    UserManager<Usuario> userManager,
    ITenantProvider tenantProvider,
    ILogger<AlterarSenhaCommandHandler> logger
) : IRequestHandler<AlterarSenhaCommand, Result>
{
    public async Task<Result> Handle(AlterarSenhaCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.NovaSenha != command.ConfirmarSenha)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A confirmação de senha falhou."));

            if (command.NovaSenha == command.SenhaAtual)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A nova senha deve ser diferente da atual."));

            Usuario? usuario = tenantProvider.UsuarioId is { } id
                ? await userManager.FindByIdAsync(id.ToString())
                : null;

            if (usuario is null)
                return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Não foi possível encontrar o usuário requisitado."));

            IdentityResult resultado = await userManager.ChangePasswordAsync(usuario, command.SenhaAtual, command.NovaSenha);

            if (!resultado.Succeeded)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(ErrosDeIdentity.Traduzir(resultado.Errors)));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a troca de senha.");

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}
