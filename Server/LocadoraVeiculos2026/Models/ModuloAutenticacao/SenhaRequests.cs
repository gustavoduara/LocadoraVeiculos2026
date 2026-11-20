namespace LocadoraDeVeiculos.WebApi.Models.ModuloAutenticacao;

public record EsqueciSenhaRequest(string Email);

/// <summary><c>Codigo</c> é o que vem no link do e-mail.</summary>
public record RedefinirSenhaRequest(string Email, string Codigo, string NovaSenha, string ConfirmarSenha);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha, string ConfirmarSenha);
