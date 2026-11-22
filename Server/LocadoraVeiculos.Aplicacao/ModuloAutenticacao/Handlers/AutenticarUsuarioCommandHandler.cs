using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Infraestrutura.Jwt.Services;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloFuncionario;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Handlers;

public class AutenticarUsuarioCommandHandler(
    UserManager<Usuario> userManager,
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    AccessTokenProvider tokenProvider,
    RefreshTokenProvider refreshTokenProvider,
    ILogger<AutenticarUsuarioCommandHandler> logger
) : IRequestHandler<AutenticarUsuarioCommand, Result<(AccessToken, RefreshToken)>>
{
    /// <summary>A mesma frase para e-mail sem conta, senha errada e conta bloqueada: não revela qual deles aconteceu.</summary>
    public const string MensagemDeFalha =
        "Login ou senha incorretos. Depois de várias tentativas erradas, a conta fica bloqueada por alguns minutos.";

    public async Task<Result<(AccessToken, RefreshToken)>> Handle(
        AutenticarUsuarioCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var usuarioEncontrado = await userManager.FindByEmailAsync(command.Email);

            // E-mail sem conta e senha errada dão a MESMA resposta (400), para o login não revelar quem tem conta.
            if (usuarioEncontrado is null)
            {
                // Gasta o mesmo tempo de uma conferência de senha, para o tempo de resposta também não denunciar.
                userManager.PasswordHasher.HashPassword(new Usuario(), command.Senha);

                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(MensagemDeFalha));
            }

            // Conta bloqueada por excesso de tentativas: mesma resposta de qualquer falha (não confere nem a senha certa).
            if (await userManager.IsLockedOutAsync(usuarioEncontrado))
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(MensagemDeFalha));

            var credenciaisValidas = await userManager.CheckPasswordAsync(
                usuarioEncontrado,
                command.Senha
            );

            if (!credenciaisValidas)
            {
                // Soma uma tentativa errada; ao chegar no limite o Identity bloqueia a conta.
                await userManager.AccessFailedAsync(usuarioEncontrado);

                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(MensagemDeFalha));
            }

            await userManager.ResetAccessFailedCountAsync(usuarioEncontrado);

            // Só depois de a senha conferir: quem não sabe a senha não descobre se o e-mail está confirmado.
            if (!usuarioEncontrado.EmailConfirmed)
                return Result.Fail(ResultadosErro.EmailNaoConfirmadoErro());

            // Só depois de a senha conferir: quem não sabe a senha não descobre o tipo da conta.
            if (command.Tipo is { } tipo)
            {
                bool ehCliente = await userManager.IsInRoleAsync(usuarioEncontrado, CargoUsuario.Cliente.ToString());

                if (tipo == TipoDeAcesso.Locadora && ehCliente)
                    return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                        "Esta conta é de cliente. Entre pela opção Cliente."));

                if (tipo == TipoDeAcesso.Cliente && !ehCliente)
                    return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                        "Esta conta é de locadora. Entre pela opção Locadora."));
            }

            if (await userManager.IsInRoleAsync(usuarioEncontrado, CargoUsuario.Funcionario.ToString()))
            {
                var funcionario = await repositorioFuncionario.SelecionarPorUsuarioIdAsync(usuarioEncontrado.Id);

                if (funcionario is null || funcionario.Excluido || !funcionario.Ativo)
                    return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(
                        "Esta conta está desativada. Procure o administrador da empresa."));
            }

            var accessToken = await tokenProvider.GerarAccessTokenAsync(usuarioEncontrado);

            var refreshToken = await refreshTokenProvider.GerarRefreshTokenAsync(usuarioEncontrado);

            return Result.Ok((accessToken, refreshToken));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante a autenticação de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}