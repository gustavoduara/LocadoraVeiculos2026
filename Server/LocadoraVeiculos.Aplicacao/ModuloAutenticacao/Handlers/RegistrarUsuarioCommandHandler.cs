using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Infraestrutura.Jwt.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Handlers;

public class RegistrarUsuarioCommandHandler(
    UserManager<Usuario> userManager,
    RoleManager<Cargo> roleManager,
    IEnviadorDeEmail enviadorDeEmail,
    ILogger<RegistrarUsuarioCommandHandler> logger
) : IRequestHandler<RegistrarUsuarioCommand, Result>
{
    public async Task<Result> Handle(
        RegistrarUsuarioCommand command, CancellationToken cancellationToken)
    {
        Usuario? usuario = null;

        try
        {
            // Só os cargos de conta aberta ao público: a locadora (Empresa) e o cliente do catálogo.
            if (command.Cargo is not (CargoUsuario.Empresa or CargoUsuario.Cliente))
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O tipo de conta informado é inválido."));

            if (!command.Senha.Equals(command.ConfirmarSenha))
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A confirmação de senha falhou."));

            usuario = new Usuario
            {
                FullName = command.NomeCompleto,
                UserName = command.Email,
                Email = command.Email
            };

            var usuarioResult = await userManager.CreateAsync(usuario, command.Senha);

            if (!usuarioResult.Succeeded)
            {
                var erros = ErrosDeIdentity.Traduzir(usuarioResult.Errors);

                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
            }

            var cargoStr = command.Cargo.ToString();

            var resultadoBuscaCargo = await roleManager.FindByNameAsync(cargoStr);

            if (resultadoBuscaCargo is null)
            {
                var cargo = new Cargo()
                {
                    Name = cargoStr,
                    NormalizedName = cargoStr.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                await roleManager.CreateAsync(cargo);
            }

            var resultadoInclusaoCargo = await userManager.AddToRoleAsync(usuario, cargoStr);

            if (!resultadoInclusaoCargo.Succeeded)
            {
                var erros = resultadoInclusaoCargo.Errors.Select(e => e.Description);

                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
            }

            // A conta só entra depois de confirmar o e-mail. Se o envio falhar, a conta continua criada:
            // a pessoa pede o reenvio na tela de login.
            try
            {
                await EmailDeConfirmacao.EnviarAsync(userManager, enviadorDeEmail, usuario, command.EnderecoDoSite, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Não foi possível enviar o e-mail de confirmação.");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            if (usuario is not null)
                await userManager.DeleteAsync(usuario);

            logger.LogError(
                ex,
                "Ocorreu um erro durante o registro de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}