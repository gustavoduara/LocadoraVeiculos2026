using Microsoft.AspNetCore.Identity;

namespace LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

public class Usuario : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public Guid AccessTokenVersionId { get; set; } = Guid.Empty;

    /// <summary>Quando a pessoa aceitou os Termos de Uso e a Política de Privacidade (vazio nas contas anteriores a eles).</summary>
    public DateTime? AceiteTermosEmUtc { get; set; }

    /// <summary>Versão dos termos que ela aceitou (<see cref="TermosDeUso.VersaoAtual"/> na hora do aceite).</summary>
    public string? VersaoTermosAceita { get; set; }

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