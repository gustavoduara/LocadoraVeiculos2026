using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Handlers;

internal static class CondutorMapeamento
{
    public static CondutorDto ParaDto(this Condutor c) => new(
        c.Id,
        c.ClienteId,
        c.Cliente?.Nome ?? string.Empty,
        c.Nome,
        c.Email,
        c.Telefone,
        c.Cpf,
        c.Cnh,
        c.ValidadeCnh,
        c.CnhVencida(DateOnly.FromDateTime(DateTime.UtcNow))
    );
}

public class CadastrarCondutorCommandHandler(
    AppDbContext appDbContext,
    RepositorioCondutorEmOrm repositorioCondutor,
    RepositorioClienteEmOrm repositorioCliente,
    ITenantProvider tenantProvider,
    IValidator<CadastrarCondutorCommand> validator,
    ILogger<CadastrarCondutorCommandHandler> logger
) : IRequestHandler<CadastrarCondutorCommand, Result<CadastrarCondutorResult>>
{
    public async Task<Result<CadastrarCondutorResult>> Handle(
        CadastrarCondutorCommand command, CancellationToken cancellationToken)
    {
        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        // O filtro por empresa garante que só clientes da própria locadora são encontrados.
        Cliente? cliente = await repositorioCliente.SelecionarPorIdAsync(command.ClienteId);

        if (cliente is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.ClienteId));

        if (await repositorioCondutor.ExisteCpfNoClienteAsync(cliente.Id, command.Cpf))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                "Já existe um condutor com este CPF cadastrado para este cliente."));

        try
        {
            Condutor condutor = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                cliente.Id,
                command.Nome.Trim(),
                command.Email.Trim(),
                command.Telefone,
                command.Cpf,
                command.Cnh,
                command.ValidadeCnh
            );

            await repositorioCondutor.CadastrarAsync(condutor);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarCondutorResult(condutor.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarCondutorCommandHandler(
    AppDbContext appDbContext,
    RepositorioCondutorEmOrm repositorioCondutor,
    ITenantProvider tenantProvider,
    IValidator<EditarCondutorCommand> validator,
    ILogger<EditarCondutorCommandHandler> logger
) : IRequestHandler<EditarCondutorCommand, Result<EditarCondutorResult>>
{
    public async Task<Result<EditarCondutorResult>> Handle(
        EditarCondutorCommand command, CancellationToken cancellationToken)
    {
        Condutor? condutor = await repositorioCondutor.SelecionarPorIdAsync(command.Id);

        if (condutor is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        ValidationResult resultadoValidacao = await validator.ValidateAsync(command, cancellationToken);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        if (await repositorioCondutor.ExisteCpfNoClienteAsync(condutor.ClienteId, command.Cpf, ignorarId: condutor.Id))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                "Já existe um condutor com este CPF cadastrado para este cliente."));

        // Quando existirem Aluguéis, bloquear aqui a edição de condutor em aluguel não concluído, se a regra se aplicar.

        try
        {
            Condutor condutorEditado = new(
                tenantProvider.EmpresaId.GetValueOrDefault(),
                condutor.ClienteId,
                command.Nome.Trim(),
                command.Email.Trim(),
                command.Telefone,
                command.Cpf,
                command.Cnh,
                command.ValidadeCnh
            );

            condutor.AtualizarRegistro(condutorEditado);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new EditarCondutorResult(condutor.ParaDto()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirCondutorCommandHandler(
    AppDbContext appDbContext,
    RepositorioCondutorEmOrm repositorioCondutor,
    ILogger<ExcluirCondutorCommandHandler> logger
) : IRequestHandler<ExcluirCondutorCommand, Result<ExcluirCondutorResult>>
{
    public async Task<Result<ExcluirCondutorResult>> Handle(
        ExcluirCondutorCommand command, CancellationToken cancellationToken)
    {
        Condutor? condutor = await repositorioCondutor.SelecionarPorIdAsync(command.Id);

        if (condutor is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        // Quando existirem Aluguéis, bloquear aqui a exclusão de condutor em aluguel não concluído.

        try
        {
            await repositorioCondutor.ExcluirAsync(condutor.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirCondutorResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarCondutorPorIdQueryHandler(
    RepositorioCondutorEmOrm repositorioCondutor
) : IRequestHandler<SelecionarCondutorPorIdQuery, Result<CondutorDto>>
{
    public async Task<Result<CondutorDto>> Handle(
        SelecionarCondutorPorIdQuery query, CancellationToken cancellationToken)
    {
        Condutor? condutor = await repositorioCondutor.SelecionarPorIdAsync(query.Id);

        if (condutor is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(condutor.ParaDto());
    }
}

public class SelecionarCondutoresQueryHandler(
    RepositorioCondutorEmOrm repositorioCondutor
) : IRequestHandler<SelecionarCondutoresQuery, Result<SelecionarCondutoresResult>>
{
    public async Task<Result<SelecionarCondutoresResult>> Handle(
        SelecionarCondutoresQuery query, CancellationToken cancellationToken)
    {
        List<Condutor> condutores = await repositorioCondutor.SelecionarTodosAsync(query.ClienteId);

        return Result.Ok(new SelecionarCondutoresResult(condutores.Select(c => c.ParaDto()).ToList()));
    }
}
