using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;

/// <summary>A pessoa abriu o link do e-mail: confirma que o endereço é dela. Repetir o pedido não dá erro.</summary>
public record ConfirmarEmailCommand(string Email, string Codigo) : IRequest<Result>;

/// <summary>
/// Manda de novo o e-mail de confirmação. A resposta é sempre a mesma, exista ou não a conta
/// (ninguém descobre quem tem conta); só quem ainda não confirmou recebe.
/// </summary>
public record ReenviarConfirmacaoCommand(string Email, string EnderecoDoSite) : IRequest<Result>;
