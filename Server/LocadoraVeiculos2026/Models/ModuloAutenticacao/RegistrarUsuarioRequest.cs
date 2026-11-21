namespace LocadoraDeVeiculos.WebApi.Models.ModuloAutenticacao;

public record RegistrarUsuarioRequest(
    string NomeCompleto,
    string Email,
    string Senha,
    string ConfirmarSenha,
    bool AceitouTermos = false
);