using Microsoft.AspNetCore.Identity;

namespace LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

public class Usuario : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public Guid AccessTokenVersionId { get; set; } = Guid.Empty;

    public Usuario()
    {
        Id = Guid.NewGuid();
        // O e-mail nasce não confirmado: a pessoa precisa abrir o link enviado (contas criadas pelo administrador
        // da empresa, como as de funcionário, são marcadas como confirmadas na hora do cadastro).
    }
}

public record UsuarioAutenticado(
    Guid Id,
    string NomeCompleto,
    string Email,
    CargoUsuario Cargo
);