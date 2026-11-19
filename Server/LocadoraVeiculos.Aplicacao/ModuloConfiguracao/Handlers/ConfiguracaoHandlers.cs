using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConfiguracao;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Handlers;

public class SelecionarConfiguracaoQueryHandler(
    RepositorioConfiguracaoEmOrm repositorioConfiguracao
) : IRequestHandler<SelecionarConfiguracaoQuery, Result<SelecionarConfiguracaoResult>>
{
    public async Task<Result<SelecionarConfiguracaoResult>> Handle(
        SelecionarConfiguracaoQuery query, CancellationToken cancellationToken)
    {
        Configuracao? configuracao = await repositorioConfiguracao.SelecionarDaEmpresaAsync();

        // Empresa que ainda não configurou: devolve zeros em vez de erro.
        SelecionarConfiguracaoResult result = configuracao is null
            ? new(0m, 0m, 0m, 0m)
            : new(
                configuracao.PrecoGasolina,
                configuracao.PrecoGas,
                configuracao.PrecoDiesel,
                configuracao.PrecoAlcool
            );

        return Result.Ok(result);
    }
}

public class EditarConfiguracaoCommandHandler(
    AppDbContext appDbContext,
    RepositorioConfiguracaoEmOrm repositorioConfiguracao,
    ITenantProvider tenantProvider,
    IValidator<EditarConfiguracaoCommand> validator,
    ILogger<EditarConfiguracaoCommandHandler> logger
) : IRequestHandler<EditarConfiguracaoCommand, Result<EditarConfiguracaoResult>>
{
    public async Task<Result<EditarConfiguracaoResult>> Handle(
        EditarConfiguracaoCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        try
        {
            Configuracao novaConfiguracao = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                command.PrecoGasolina,
                command.PrecoGas,
                command.PrecoDiesel,
                command.PrecoAlcool
            );

            Configuracao? existente = await repositorioConfiguracao.SelecionarDaEmpresaAsync();

            // Upsert: a primeira gravação cria o registro, as seguintes o atualizam.
            if (existente is null)
                await repositorioConfiguracao.CadastrarAsync(novaConfiguracao);
            else
                existente.AtualizarRegistro(novaConfiguracao);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarConfiguracaoResult(
                command.PrecoGasolina,
                command.PrecoGas,
                command.PrecoDiesel,
                command.PrecoAlcool
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ocorreu um erro durante a edição de {@Command}.",
                command
            );

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}
