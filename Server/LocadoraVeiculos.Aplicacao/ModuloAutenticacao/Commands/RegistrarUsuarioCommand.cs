using FluentResults;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands;

/// <summary>
/// Cria uma conta aberta ao público (locadora ou cliente). A conta só entra depois de confirmar o e-mail:
/// <c>EnderecoDoSite</c> é a base do link que vai no e-mail; <c>AceitouTermos</c> é o aceite dos Termos de Uso e da
/// Política de Privacidade (sem ele a conta não é criada).
/// </summary>
public record RegistrarUsuarioCommand(
    string NomeCompleto,
    string Email,
    string Senha,
    string ConfirmarSenha,
    string EnderecoDoSite,
    bool AceitouTermos,
    CargoUsuario Cargo = CargoUsuario.Empresa
) : IRequest<Result>;
