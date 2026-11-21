using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Infraestrutura.Jwt.Services;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Handlers;

public class SairCommandHandler(
    AppDbContext dbContext,
    UserManager<Usuario> userManager,
    RefreshTokenProvider refreshTokenProvider,
    ILogger<SairCommandHandler> logger
) : IRequestHandler<SairCommand, Result>
{
    public async Task<Result> Handle(SairCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == command.RefreshTokenHash, cancellationToken);

            // Sessão que já não existe (cookie velho): sair é o que a pessoa quer, então não é erro.
            if (token is null)
                return Result.Ok();

            var usuarioId = token.UsuarioId;

            await refreshTokenProvider.RevogarTokensUsuarioAsync(usuarioId, "Logout");

            var usuarioEncontrado = await userManager.FindByIdAsync(usuarioId.ToString());

            if (usuarioEncontrado is null)
                return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Não foi possível encontrar o usuário requisitado."));

            usuarioEncontrado.AccessTokenVersionId = Guid.NewGuid();

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante o logout {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}