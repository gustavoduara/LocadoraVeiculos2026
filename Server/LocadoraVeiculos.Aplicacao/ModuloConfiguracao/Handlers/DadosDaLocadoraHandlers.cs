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

public class SelecionarDadosDaLocadoraQueryHandler(
    RepositorioConfiguracaoEmOrm repositorio
) : IRequestHandler<SelecionarDadosDaLocadoraQuery, Result<DadosDaLocadoraDto>>
{
    public async Task<Result<DadosDaLocadoraDto>> Handle(SelecionarDadosDaLocadoraQuery query, CancellationToken cancellationToken)
    {
        Configuracao? configuracao = await repositorio.SelecionarDaEmpresaAsync();

        return Result.Ok(new DadosDaLocadoraDto(configuracao?.NomeFantasia, configuracao?.Telefone));
    }
}

public class EditarDadosDaLocadoraCommandHandler(
    AppDbContext appDbContext,
    RepositorioConfiguracaoEmOrm repositorio,
    ITenantProvider tenantProvider,
    IValidator<EditarDadosDaLocadoraCommand> validator,
    ILogger<EditarDadosDaLocadoraCommandHandler> logger
) : IRequestHandler<EditarDadosDaLocadoraCommand, Result<DadosDaLocadoraDto>>
{
    public async Task<Result<DadosDaLocadoraDto>> Handle(EditarDadosDaLocadoraCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validacao = await validator.ValidateAsync(command, cancellationToken);

        if (!validacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(validacao.Errors.Select(e => e.ErrorMessage)));

        try
        {
            Configuracao? configuracao = await repositorio.SelecionarDaEmpresaAsync();

            if (configuracao is null)
            {
                // Ainda não há preços configurados: cria a linha só com os dados da locadora (preços em zero).
                configuracao = new Configuracao(tenantProvider.EmpresaId.GetValueOrDefault(), 0m, 0m, 0m, 0m, 0m, 0m);

                await repositorio.CadastrarAsync(configuracao);
            }

            configuracao.AtualizarDadosDaLocadora(command.NomeFantasia, command.Telefone);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new DadosDaLocadoraDto(configuracao.NomeFantasia, configuracao.Telefone));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}
