using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.WebApi.Compartilhado;
using LocadoraDeVeiculos.WebApi.Config.Identity;
using LocadoraDeVeiculos.WebApi.Models.ModuloAutenticacao;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LocadoraDeVeiculos.WebApi.Config.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocadoraDeVeiculos.WebApi.Controllers;

[Route("api/auth")]
public class AutenticacaoController(IMediator mediator) : MainController
{
    [EnableRateLimiting(SegurancaConfig.PoliticaDeCadastro)]
    [HttpPost("registrar")]
    public async Task<ActionResult> Registrar(RegistrarUsuarioRequest request, [FromServices] IConfiguration configuracao)
    {
        var command = new RegistrarUsuarioCommand(
            request.NomeCompleto,
            request.Email,
            request.Senha,
            request.ConfirmarSenha,
            EnderecoDoSite(configuracao),
            request.AceitouTermos
        );

        var result = await mediator.Send(command);

        return ProcessarResultado(result, () => ResponderCadastro(request.Email));
    }

    /// <summary>Conta de cliente: quem procura e aluga carros no catálogo (não é uma locadora).</summary>
    [EnableRateLimiting(SegurancaConfig.PoliticaDeCadastro)]
    [HttpPost("registrar-cliente")]
    public async Task<ActionResult> RegistrarCliente(RegistrarUsuarioRequest request, [FromServices] IConfiguration configuracao)
    {
        var command = new RegistrarUsuarioCommand(
            request.NomeCompleto,
            request.Email,
            request.Senha,
            request.ConfirmarSenha,
            EnderecoDoSite(configuracao),
            request.AceitouTermos,
            CargoUsuario.Cliente
        );

        var result = await mediator.Send(command);

        return ProcessarResultado(result, () => ResponderCadastro(request.Email));
    }

    /// <summary>A pessoa abriu o link do e-mail: confirma o endereço. Pode repetir sem erro.</summary>
    [EnableRateLimiting(SegurancaConfig.PoliticaDeRecuperacao)]
    [HttpPost("confirmar-email")]
    public async Task<ActionResult> ConfirmarEmail(ConfirmarEmailRequest request)
    {
        var result = await mediator.Send(new ConfirmarEmailCommand(request.Email, request.Codigo));

        return ProcessarResultado(result, () => NoContent());
    }

    /// <summary>Manda de novo o e-mail de confirmação. A resposta é sempre a mesma.</summary>
    [EnableRateLimiting(SegurancaConfig.PoliticaDeRecuperacao)]
    [HttpPost("reenviar-confirmacao")]
    public async Task<ActionResult> ReenviarConfirmacao(ReenviarConfirmacaoRequest request, [FromServices] IConfiguration configuracao)
    {
        var result = await mediator.Send(new ReenviarConfirmacaoCommand(request.Email, EnderecoDoSite(configuracao)));

        return ProcessarResultado(result, () => Ok(new
        {
            mensagem = "Se a conta existir e ainda não estiver confirmada, enviamos um novo link para o e-mail."
        }));
    }

    [EnableRateLimiting(SegurancaConfig.PoliticaDeLogin)]
    [HttpPost("autenticar")]
    public async Task<ActionResult<AccessToken>> Autenticar(AutenticarUsuarioRequest request)
    {
        var command = new AutenticarUsuarioCommand(request.Email, request.Senha, request.Tipo);

        var result = await mediator.Send(command);

        return ProcessarResultado(result, ResponderComToken);
    }

    /// <summary>Envia o e-mail com o link para criar uma nova senha. A resposta é sempre a mesma.</summary>
    [EnableRateLimiting(SegurancaConfig.PoliticaDeRecuperacao)]
    [HttpPost("esqueci-senha")]
    public async Task<ActionResult> EsqueciSenha(EsqueciSenhaRequest request, [FromServices] IConfiguration configuracao)
    {
        var result = await mediator.Send(new SolicitarRedefinicaoSenhaCommand(request.Email, EnderecoDoSite(configuracao)));

        return ProcessarResultado(result, () => Ok(new
        {
            mensagem = "Se o e-mail estiver cadastrado, enviamos um link para criar uma nova senha."
        }));
    }

    [EnableRateLimiting(SegurancaConfig.PoliticaDeRecuperacao)]
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

        // Cookie que não vale mais: apaga do navegador para ele não ser reenviado a cada visita.
        if (result.IsFailed && result.Errors.Any(e => e.Metadata.TryGetValue("TipoErro", out var tipo) && tipo as string == "NaoAutorizado"))
            RefreshTokenCookieService.LimparCookie(Response);

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

    /// <summary>APP_URL (se existir) vence: atrás de um proxy o endereço público pode ser outro.</summary>
    private string EnderecoDoSite(IConfiguration configuracao) =>
        configuracao["APP_URL"] ?? $"{Request.Scheme}://{Request.Host}";

    private ActionResult ResponderCadastro(string email) => Ok(new
    {
        email,
        mensagem = "Conta criada! Enviamos um e-mail de confirmação: abra o link para poder entrar."
    });

    private ActionResult ResponderComToken((AccessToken AccessToken, RefreshToken RefreshToken) valor)
    {
        RefreshTokenCookieService.EnviarCookie(Response, valor.RefreshToken);

        return Ok(valor.AccessToken);
    }
}