using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;

/// <summary>
/// Pede o e-mail com o link para criar uma nova senha. O resultado é sempre o mesmo, exista ou não a conta,
/// para ninguém descobrir quem tem conta no sistema.
/// </summary>
public record SolicitarRedefinicaoSenhaCommand(string Email, string EnderecoDoSite) : IRequest<Result>;

public record RedefinirSenhaCommand(string Email, string Codigo, string NovaSenha, string ConfirmarSenha)
    : IRequest<Result>;

/// <summary>A pessoa logada troca a própria senha, informando a atual.</summary>
public record AlterarSenhaCommand(string SenhaAtual, string NovaSenha, string ConfirmarSenha) : IRequest<Result>;
