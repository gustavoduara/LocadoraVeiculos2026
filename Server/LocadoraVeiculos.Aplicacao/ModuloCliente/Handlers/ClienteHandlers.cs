using FluentResults;
using FluentValidation.Results;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Validators;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCliente.Handlers;

internal static class ClienteRegras
{
    public static ValidationResult Validar(TipoCliente tipo, DadosClienteDto dados)
    {
        return tipo == TipoCliente.PessoaFisica
            ? new PessoaFisicaDadosValidator().Validate(dados)
            : new PessoaJuridicaDadosValidator().Validate(dados);
    }

    /// <summary>Vínculo opcional de uma pessoa física: precisa ser uma pessoa jurídica da própria empresa.</summary>
    public static async Task<Error?> ValidarVinculoAsync(RepositorioClienteEmOrm repositorio, Guid? pessoaJuridicaId)
    {
        if (pessoaJuridicaId is null)
            return null;

        // O filtro por empresa garante que só clientes da própria locadora são encontrados.
        Cliente? pessoaJuridica = await repositorio.SelecionarPorIdAsync(pessoaJuridicaId.Value);

        if (pessoaJuridica is null)
            return ResultadosErro.RegistroNaoEncontradoErro(pessoaJuridicaId.Value);

        if (pessoaJuridica.Tipo != TipoCliente.PessoaJuridica)
            return ResultadosErro.RequisicaoInvalidaErro("O vínculo deve ser feito com uma pessoa jurídica.");

        return null;
    }

    public static Cliente Montar(
        TipoCliente tipo, Guid empresaId, DadosClienteDto d, Guid? pessoaJuridicaId)
    {
        string nome = d.Nome.Trim();
        string email = d.Email.Trim();
        string estado = d.Estado.Trim().ToUpperInvariant();
        string cidade = d.Cidade.Trim();
        string bairro = d.Bairro.Trim();
        string rua = d.Rua.Trim();
        string numero = d.Numero.Trim();

        return tipo == TipoCliente.PessoaFisica
            ? Cliente.NovaPessoaFisica(
                empresaId, nome, email, d.Telefone, d.Cpf!, d.Rg!.Trim(), d.Cnh!,
                estado, cidade, bairro, rua, numero, pessoaJuridicaId)
            : Cliente.NovaPessoaJuridica(
                empresaId, nome, email, d.Telefone, d.Cnpj!,
                estado, cidade, bairro, rua, numero);
    }

    public static ClienteDto ParaDto(this Cliente c) => new(
        c.Id,
        c.Tipo,
        c.Nome,
        c.Email,
        c.Telefone,
        c.Cpf,
        c.Rg,
        c.Cnh,
        c.Cnpj,
        c.Estado,
        c.Cidade,
        c.Bairro,
        c.Rua,
        c.Numero,
        c.PessoaJuridicaId,
        c.PessoaJuridica?.Nome
    );
}

public class CadastrarClienteCommandHandler(
    AppDbContext appDbContext,
    RepositorioClienteEmOrm repositorioCliente,
    ITenantProvider tenantProvider,
    ILogger<CadastrarClienteCommandHandler> logger
) : IRequestHandler<CadastrarClienteCommand, Result<CadastrarClienteResult>>
{
    public async Task<Result<CadastrarClienteResult>> Handle(
        CadastrarClienteCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.Tipo))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O tipo de cliente informado é inválido."));

        ValidationResult resultadoValidacao = ClienteRegras.Validar(command.Tipo, command.Dados);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        Guid? vinculo = null;

        if (command.Tipo == TipoCliente.PessoaFisica)
        {
            if (await repositorioCliente.ExisteCpfAsync(command.Dados.Cpf!))
                return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um cliente cadastrado com este CPF."));

            Error? erroVinculo = await ClienteRegras.ValidarVinculoAsync(repositorioCliente, command.Dados.PessoaJuridicaId);

            if (erroVinculo is not null)
                return Result.Fail(erroVinculo);

            vinculo = command.Dados.PessoaJuridicaId;
        }
        else if (await repositorioCliente.ExisteCnpjAsync(command.Dados.Cnpj!))
        {
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um cliente cadastrado com este CNPJ."));
        }

        try
        {
            Cliente cliente = ClienteRegras.Montar(
                command.Tipo, tenantProvider.EmpresaId.GetValueOrDefault(), command.Dados, vinculo);

            await repositorioCliente.CadastrarAsync(cliente);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CadastrarClienteResult(cliente.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante o cadastro de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class EditarClienteCommandHandler(
    AppDbContext appDbContext,
    RepositorioClienteEmOrm repositorioCliente,
    ITenantProvider tenantProvider,
    ILogger<EditarClienteCommandHandler> logger
) : IRequestHandler<EditarClienteCommand, Result<EditarClienteResult>>
{
    public async Task<Result<EditarClienteResult>> Handle(
        EditarClienteCommand command, CancellationToken cancellationToken)
    {
        Cliente? cliente = await repositorioCliente.SelecionarPorIdAsync(command.Id);

        if (cliente is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        // O tipo é o do registro existente: pessoa física continua física, jurídica continua jurídica.
        ValidationResult resultadoValidacao = ClienteRegras.Validar(cliente.Tipo, command.Dados);

        if (!resultadoValidacao.IsValid)
        {
            var erros = resultadoValidacao.Errors.Select(e => e.ErrorMessage);

            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(erros));
        }

        Guid? vinculo = null;

        if (cliente.Tipo == TipoCliente.PessoaFisica)
        {
            if (await repositorioCliente.ExisteCpfAsync(command.Dados.Cpf!, ignorarId: cliente.Id))
                return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um cliente cadastrado com este CPF."));

            Error? erroVinculo = await ClienteRegras.ValidarVinculoAsync(repositorioCliente, command.Dados.PessoaJuridicaId);

            if (erroVinculo is not null)
                return Result.Fail(erroVinculo);

            vinculo = command.Dados.PessoaJuridicaId;
        }
        else if (await repositorioCliente.ExisteCnpjAsync(command.Dados.Cnpj!, ignorarId: cliente.Id))
        {
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro("Já existe um cliente cadastrado com este CNPJ."));
        }


        try
        {
            Cliente clienteEditado = ClienteRegras.Montar(
                cliente.Tipo, tenantProvider.EmpresaId.GetValueOrDefault(), command.Dados, vinculo);

            cliente.AtualizarRegistro(clienteEditado);

            await appDbContext.SaveChangesAsync(cancellationToken);

            // Recarrega o vínculo para devolver o nome da pessoa jurídica atualizado.
            Cliente? atualizado = await repositorioCliente.SelecionarPorIdAsync(cliente.Id);

            return Result.Ok(new EditarClienteResult((atualizado ?? cliente).ParaDto()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a edição de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class ExcluirClienteCommandHandler(
    AppDbContext appDbContext,
    RepositorioClienteEmOrm repositorioCliente,
    RepositorioAluguelEmOrm repositorioAluguel,
    RepositorioCondutorEmOrm repositorioCondutor,
    ILogger<ExcluirClienteCommandHandler> logger
) : IRequestHandler<ExcluirClienteCommand, Result<ExcluirClienteResult>>
{
    public async Task<Result<ExcluirClienteResult>> Handle(
        ExcluirClienteCommand command, CancellationToken cancellationToken)
    {
        Cliente? cliente = await repositorioCliente.SelecionarPorIdAsync(command.Id);

        if (cliente is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(command.Id));

        if (await repositorioCondutor.ExisteParaClienteAsync(cliente.Id))
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir um cliente que possui condutores cadastrados."));

        if (cliente.Tipo == TipoCliente.PessoaJuridica &&
            await repositorioCliente.ExistePessoaFisicaVinculadaAsync(cliente.Id))
        {
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir uma pessoa jurídica que possui pessoas físicas vinculadas."));
        }

        if (await repositorioAluguel.ExisteEmAbertoParaClienteAsync(cliente.Id))
            return Result.Fail(ResultadosErro.ExclusaoBloqueadaErro(
                "Não é possível excluir um cliente relacionado a um aluguel ainda não concluído."));


        try
        {
            await repositorioCliente.ExcluirAsync(cliente.Id);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(new ExcluirClienteResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a exclusão de {@Command}.", command);

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}

public class SelecionarClientePorIdQueryHandler(
    RepositorioClienteEmOrm repositorioCliente
) : IRequestHandler<SelecionarClientePorIdQuery, Result<ClienteDto>>
{
    public async Task<Result<ClienteDto>> Handle(
        SelecionarClientePorIdQuery query, CancellationToken cancellationToken)
    {
        Cliente? cliente = await repositorioCliente.SelecionarPorIdAsync(query.Id);

        if (cliente is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.Id));

        return Result.Ok(cliente.ParaDto());
    }
}

public class SelecionarClientesQueryHandler(
    RepositorioClienteEmOrm repositorioCliente
) : IRequestHandler<SelecionarClientesQuery, Result<SelecionarClientesResult>>
{
    public async Task<Result<SelecionarClientesResult>> Handle(
        SelecionarClientesQuery query, CancellationToken cancellationToken)
    {
        List<Cliente> clientes = await repositorioCliente.SelecionarTodosAsync(query.Tipo);

        return Result.Ok(new SelecionarClientesResult(clientes.Select(c => c.ParaDto()).ToList()));
    }
}
