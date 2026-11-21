using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Handlers;
using LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Validators;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAluguel;

/// <summary>O cliente e o condutor criados junto com um aluguel.</summary>
public record ClienteECondutorNovos(Cliente Cliente, Condutor Condutor);

/// <summary>
/// Um cliente só passa a existir na locadora quando faz um aluguel: este serviço cadastra o cliente
/// (e o condutor) no momento de registrar o aluguel. Se o aluguel não puder ser registrado, quem foi
/// criado aqui é desfeito, para não sobrarem clientes que nunca alugaram.
/// </summary>
public class ClienteDoAluguelServico(
    AppDbContext appDbContext,
    RepositorioClienteEmOrm repositorioCliente,
    RepositorioCondutorEmOrm repositorioCondutor,
    ITenantProvider tenantProvider,
    ILogger<ClienteDoAluguelServico> logger
)
{
    public async Task<Result<ClienteECondutorNovos>> CadastrarAsync(
        NovoClienteNoAluguelDto novo, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(novo.Tipo))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("O tipo de cliente informado é inválido."));

        var validacaoCliente = ClienteRegras.Validar(novo.Tipo, novo.Cliente);

        if (!validacaoCliente.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(validacaoCliente.Errors.Select(e => e.ErrorMessage)));

        // Pessoa física dirige o próprio aluguel (só informa a validade da CNH); pessoa jurídica indica o condutor.
        DadosCondutorNoAluguelDto c = novo.Condutor;
        bool fisica = novo.Tipo == TipoCliente.PessoaFisica;

        if (!fisica && string.IsNullOrWhiteSpace(c.Nome))
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("Informe os dados do condutor do cliente."));

        CadastrarCondutorCommand comandoCondutor = new(
            Guid.NewGuid(), // o cliente ainda não existe; só para validar os demais campos
            fisica ? novo.Cliente.Nome : c.Nome ?? string.Empty,
            fisica ? novo.Cliente.Email : c.Email ?? string.Empty,
            fisica ? novo.Cliente.Telefone : c.Telefone ?? string.Empty,
            fisica ? novo.Cliente.Cpf ?? string.Empty : c.Cpf ?? string.Empty,
            fisica ? novo.Cliente.Cnh ?? string.Empty : c.Cnh ?? string.Empty,
            c.ValidadeCnh);

        var validacaoCondutor = new CadastrarCondutorCommandValidator().Validate(comandoCondutor);

        if (!validacaoCondutor.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(validacaoCondutor.Errors.Select(e => e.ErrorMessage)));

        if (fisica && await repositorioCliente.ExisteCpfAsync(novo.Cliente.Cpf!))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                "Já existe um cliente com este CPF. Selecione-o na lista de clientes."));

        if (!fisica && await repositorioCliente.ExisteCnpjAsync(novo.Cliente.Cnpj!))
            return Result.Fail(ResultadosErro.RegistroDuplicadoErro(
                "Já existe um cliente com este CNPJ. Selecione-o na lista de clientes."));

        Guid empresaId = tenantProvider.EmpresaId.GetValueOrDefault();

        Cliente cliente = ClienteRegras.Montar(novo.Tipo, empresaId, novo.Cliente, null);

        Condutor condutor = new(
            empresaId, cliente.Id, comandoCondutor.Nome, comandoCondutor.Email, comandoCondutor.Telefone,
            comandoCondutor.Cpf, comandoCondutor.Cnh, comandoCondutor.ValidadeCnh);

        await repositorioCliente.CadastrarAsync(cliente);
        await repositorioCondutor.CadastrarAsync(condutor);

        // Salva antes para o aluguel enxergar o cliente e o condutor.
        await appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new ClienteECondutorNovos(cliente, condutor));
    }

    /// <summary>Apaga o cliente e o condutor criados para um aluguel que não chegou a ser registrado.</summary>
    public async Task DesfazerAsync(ClienteECondutorNovos novos, CancellationToken cancellationToken)
    {
        try
        {
            appDbContext.Remove(novos.Condutor);
            appDbContext.Remove(novos.Cliente);

            await appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Não foi possível desfazer o cadastro do cliente {ClienteId}.", novos.Cliente.Id);
        }
    }
}
