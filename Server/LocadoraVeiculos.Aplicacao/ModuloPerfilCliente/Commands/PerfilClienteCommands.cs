using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPerfilCliente.Commands;

/// <summary>Dados do cliente para poder reservar. Datas em yyyy-MM-dd.</summary>
public record DadosPerfilClienteDto(
    string Cpf,
    string Rg,
    string Cnh,
    DateOnly ValidadeCnh,
    string Telefone,
    string Estado,
    string Cidade,
    string Bairro,
    string Rua,
    string Numero
);

/// <summary><c>Completo</c> = a pessoa já preencheu tudo e pode reservar. Sem perfil, os campos vêm vazios.</summary>
public record PerfilClienteDto(
    bool Completo,
    string Cpf,
    string Rg,
    string Cnh,
    DateOnly? ValidadeCnh,
    bool CnhVencida,
    string Telefone,
    string Estado,
    string Cidade,
    string Bairro,
    string Rua,
    string Numero
);

public record SelecionarMeusDadosQuery() : IRequest<Result<PerfilClienteDto>>;

public record SalvarMeusDadosCommand(DadosPerfilClienteDto Dados) : IRequest<Result<PerfilClienteDto>>;
