using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloFuncionario;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloFuncionario;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Handlers;

internal static class MeuPerfilExtensoes
{
    /// <summary>
    /// Localiza o funcionário do usuário logado, garantindo que pertence à empresa do token,
    /// não foi excluído e está ativo. Nunca aceita um id vindo do cliente.
    /// </summary>
    public static async Task<Funcionario?> SelecionarFuncionarioLogadoAsync(
        this RepositorioFuncionarioEmOrm repositorio, ITenantProvider tenantProvider)
    {
        if (tenantProvider.UsuarioId is null)
            return null;

        Funcionario? funcionario = await repositorio.SelecionarPorUsuarioIdAsync(tenantProvider.UsuarioId.Value);

        if (funcionario is null || funcionario.Excluido || !funcionario.Ativo)
            return null;

        if (funcionario.EmpresaId != tenantProvider.EmpresaId.GetValueOrDefault())
            return null;

        return funcionario;
    }
}

public class SelecionarMeuPerfilQueryHandler(
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    ITenantProvider tenantProvider
) : IRequestHandler<SelecionarMeuPerfilQuery, Result<SelecionarMeuPerfilResult>>
{
    public async Task<Result<SelecionarMeuPerfilResult>> Handle(
        SelecionarMeuPerfilQuery query, CancellationToken cancellationToken)
    {
        Funcionario? funcionario = await repositorioFuncionario.SelecionarFuncionarioLogadoAsync(tenantProvider);

        if (funcionario is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Perfil de funcionário do usuário logado."));

        return Result.Ok(new SelecionarMeuPerfilResult(
            funcionario.Id,
            funcionario.NomeCompleto,
            funcionario.Cpf,
            funcionario.Email,
            funcionario.Salario,
            funcionario.AdmissaoEmUtc
        ));
    }
}

public class EditarMeuPerfilCommandHandler(
    AppDbContext dbContext,
    RepositorioFuncionarioEmOrm repositorioFuncionario,
    ITenantProvider tenantProvider,
    IValidator<EditarMeuPerfilCommand> validator,
    ILogger<EditarMeuPerfilCommandHandler> logger
) : IRequestHandler<EditarMeuPerfilCommand, Result<EditarMeuPerfilResult>>
{
    public async Task<Result<EditarMeuPerfilResult>> Handle(
        EditarMeuPerfilCommand command, CancellationToken cancellationToken)
    {
        Funcionario? funcionario = await repositorioFuncionario.SelecionarFuncionarioLogadoAsync(tenantProvider);

        if (funcionario is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Perfil de funcionário do usuário logado."));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        List<Funcionario> registros = await repositorioFuncionario.SelecionarTodosAsync();

        if (registros.Any(x => !x.Id.Equals(funcionario.Id) && x.Cpf.Equals(command.Cpf)))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Um funcionário com este CPF já está cadastrado."));

        try
        {
            // Somente nome e CPF: salário e data de admissão continuam restritos ao administrador.
            funcionario.AtualizarPerfil(command.NomeCompleto, command.Cpf);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarMeuPerfilResult(funcionario.NomeCompleto, funcionario.Cpf));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante a edição do perfil {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}
