using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;
using LocadoraDeVeiculos.Dominio.ModuloCliente;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloCliente;

/// <summary>O tipo do cliente não muda na edição.</summary>
public record EditarClienteRequest(
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

public record SelecionarClientesResponse(IReadOnlyList<ClienteDto> Registros);
