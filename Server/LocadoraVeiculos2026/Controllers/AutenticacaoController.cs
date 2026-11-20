using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Config.Identity;
using LocadoraDeVeiculos.WebApi.Models.ModuloAutenticacao;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/auth")]
public class AutenticacaoController(IMediator mediator) : MainController
{
    [HttpPost("registrar")]
    public async Task<ActionResult<AccessToken>> Registrar(RegistrarUsuarioRequest request)
    {
        var command = new RegistrarUsuarioCommand(
            request.NomeCompleto,
            request.Email,
            request.Senha,
            request.ConfirmarSenha
        );

        var result = await mediator.Send(command);

        return ProcessarResultado(result, ResponderComToken);
    }

    /// <summary>Conta de cliente: quem procura e aluga carros no catálogo (não é uma locadora).</summary>
    [HttpPost("registrar-cliente")]
    public async Task<ActionResult<AccessToken>> RegistrarCliente(RegistrarUsuarioRequest request)
    {
        var command = new RegistrarUsuarioCommand(
            request.NomeCompleto,
            request.Email,
            request.Senha,
            request.ConfirmarSenha,
            CargoUsuario.Cliente
        );

        var result = await mediator.Send(command);

        return ProcessarResultado(result, ResponderComToken);
    }

    [HttpPost("autenticar")]
    public async Task<ActionResult<AccessToken>> Autenticar(AutenticarUsuarioRequest request)
    {
        var command = new AutenticarUsuarioCommand(request.Email, request.Senha, request.Tipo);

        var result = await mediator.Send(command);

        return ProcessarResultado(result, ResponderComToken);
    }

    /// <summary>Envia o e-mail com o link para criar uma nova senha. A resposta é sempre a mesma.</summary>
    [HttpPost("esqueci-senha")]
    public async Task<ActionResult> EsqueciSenha(EsqueciSenhaRequest request, [FromServices] IConfiguration configuracao)
    {
        // APP_URL (se existir) vence: atrás de um proxy o endereço público pode ser outro.
        string endereco = configuracao["APP_URL"] ?? $"{Request.Scheme}://{Request.Host}";

        var result = await mediator.Send(new SolicitarRedefinicaoSenhaCommand(request.Email, endereco));

        return ProcessarResultado(result, () => Ok(new
        {
            mensagem = "Se o e-mail estiver cadastrado, enviamos um link para criar uma nova senha."
        }));
    }

    [HttpPost("redefinir-senha")]
    public async Task<ActionResult> RedefinirSenha(RedefinirSenhaRequest request)
    {
        var result = await mediator.Send(new RedefinirSenhaCommand(
            request.Email, request.Codigo, request.NovaSenha, request.ConfirmarSenha));

        return ProcessarResultado(result, () => NoContent());
    }

    /// <summary>A pessoa logada (qualquer tipo de conta) troca a própria senha.</summary>
    [Authorize]
    [HttpPost("alterar-senha")]
    public async Task<ActionResult> AlterarSenha(AlterarSenhaRequest request)
    {
        var result = await mediator.Send(new AlterarSenhaCommand(
            request.SenhaAtual, request.NovaSenha, request.ConfirmarSenha));

        return ProcessarResultado(result, () => NoContent());
    }

    [HttpPost("rotacionar")]
    public async Task<ActionResult<AccessToken>> Rotacionar()
    {
        var refreshToken = RefreshTokenCookieService.Get(Request);

        if (refreshToken is null)
            return Unauthorized("O token de rotação não foi encontrado.");

        var result = await mediator.Send(new RotacionarTokenCommand(refreshToken));

        return ProcessarResultado(result, ResponderComToken);
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        var refreshTokenHash = RefreshTokenCookieService.Get(Request);

        if (refreshTokenHash is null)
            return Unauthorized("O token de rotação não foi encontrado.");

        var result = await mediator.Send(new SairCommand(refreshTokenHash));

        return ProcessarResultado(result, () =>
        {
            RefreshTokenCookieService.LimparCookie(Response);

            return NoContent();
        });
    }

    private ActionResult ResponderComToken((AccessToken AccessToken, RefreshToken RefreshToken) valor)
    {
        RefreshTokenCookieService.EnviarCookie(Response, valor.RefreshToken);

        return Ok(valor.AccessToken);
    }
}