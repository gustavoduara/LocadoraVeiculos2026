namespace LocadoraDeVeiculos.WebApi.Models.ModuloAutenticacao;

public record EsqueciSenhaRequest(string Email);

/// <summary><c>Codigo</c> é o que vem no link do e-mail.</summary>
public record RedefinirSenhaRequest(string Email, string Codigo, string NovaSenha, string ConfirmarSenha);

/// <summary><c>Codigo</c> é o que vem no link do e-mail de confirmação.</summary>
public record ConfirmarEmailRequest(string Email, string Codigo);

public record ReenviarConfirmacaoRequest(string Email);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha, string ConfirmarSenha);
