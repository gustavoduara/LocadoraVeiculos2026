using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Commands;
using LocadoraDeVeiculos.Dominio.ModuloFuncionario;
using LocadoraDeVeiculos.Infraestrutura.Jwt.Services;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloFuncionario;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Handlers;

public class AlterarStatusFuncionarioCommandHandler(
    AppDbContext dbContext,
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    RefreshTokenProvider refreshTokenProvider,
    ILogger<AlterarStatusFuncionarioCommandHandler> logger
) : IRequestHandler<AlterarStatusFuncionarioCommand, Result<AlterarStatusFuncionarioResult>>
{
    public async Task<Result<AlterarStatusFuncionarioResult>> Handle(
        AlterarStatusFuncionarioCommand command, CancellationToken cancellationToken)
    {
        try
        {
            Funcionario? funcionario = await repositorioFuncionario.SelecionarPorIdAsync(command.Id);

            if (funcionario is null)
                return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

            funcionario.DefinirStatus(command.Ativo);

            if (!command.Ativo && funcionario.Usuario is not null)
            {
                // Invalida os access tokens já emitidos (jti != versão atual) e os refresh tokens ativos.
                funcionario.Usuario.AccessTokenVersionId = Guid.NewGuid();

                await refreshTokenProvider.RevogarTokensUsuarioAsync(funcionario.UsuarioId, "Desativação");
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new AlterarStatusFuncionarioResult(funcionario.Id, funcionario.Ativo));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante a alteração de status de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}
