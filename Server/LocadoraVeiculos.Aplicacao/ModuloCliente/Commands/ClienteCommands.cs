using FluentResults;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;

/// <summary>
/// Dados do cliente. Cpf, Rg, Cnh e PessoaJuridicaId valem para pessoa física; Cnpj para pessoa jurídica
/// (os campos que não pertencem ao tipo do cliente são ignorados).
/// </summary>
public record DadosClienteDto(
    string Nome,
    string Email,
    string Telefone,
    string? Cpf,
    string? Rg,
    string? Cnh,
    string? Cnpj,
    string Estado,
    string Cidade,
    string Bairro,
    string Rua,
    string Numero,
    Guid? PessoaJuridicaId
);

public record ClienteDto(
    Guid Id,
    TipoCliente Tipo,
    string Nome,
    string Email,
    string Telefone,
    string? Cpf,
    string? Rg,
    string? Cnh,
    string? Cnpj,
    string Estado,
    string Cidade,
    string Bairro,
    string Rua,
    string Numero,
    Guid? PessoaJuridicaId,
    string? NomePessoaJuridica
);

public record CadastrarClienteCommand(TipoCliente Tipo, DadosClienteDto Dados)
    : IRequest<Result<CadastrarClienteResult>>;

public record CadastrarClienteResult(Guid Id);

/// <summary>O tipo do cliente não pode ser alterado depois do cadastro.</summary>
public record EditarClienteCommand(Guid Id, DadosClienteDto Dados)
    : IRequest<Result<EditarClienteResult>>;

public record EditarClienteResult(ClienteDto Cliente);

public record ExcluirClienteCommand(Guid Id) : IRequest<Result<ExcluirClienteResult>>;

public record ExcluirClienteResult();

public record SelecionarClientePorIdQuery(Guid Id) : IRequest<Result<ClienteDto>>;

public record SelecionarClientesQuery(TipoCliente? Tipo = null)
    : IRequest<Result<SelecionarClientesResult>>;

public record SelecionarClientesResult(IReadOnlyList<ClienteDto> Registros);
