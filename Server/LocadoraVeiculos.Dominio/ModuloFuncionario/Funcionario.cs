using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

namespace LocadoraDeVeiculos.Dominio.ModuloFuncionario;

public class Funcionario : EntidadeBase<Funcionario>
{
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public string Email { get; set; }
    public decimal Salario { get; set; }
    public DateTimeOffset AdmissaoEmUtc { get; set; }
    public bool Ativo { get; set; } = true;

    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    protected Funcionario() { }

    public Funcionario(
        Guid usuarioId,
        Guid tenantId,
        string nomeCompleto,
        string cpf,
        string email,
        decimal salario,
        DateTimeOffset admissaoEmUtc
    )
    {
        UsuarioId = usuarioId;
        EmpresaId = tenantId;
        NomeCompleto = nomeCompleto;
        Cpf = cpf;
        Email = email;
        Salario = salario;
        AdmissaoEmUtc = admissaoEmUtc;
    }

    public void AtualizarPerfil(string nomeCompleto, string cpf)
    {
        NomeCompleto = nomeCompleto;
        Cpf = cpf;

        if (Usuario is not null)
            Usuario.FullName = nomeCompleto;
    }

    public void DefinirStatus(bool ativo)
    {
        Ativo = ativo;
    }

    public override void AtualizarRegistro(Funcionario registroEditado)
    {
        NomeCompleto = registroEditado.NomeCompleto;
        Cpf = registroEditado.Cpf;
        Salario = registroEditado.Salario;
        AdmissaoEmUtc = registroEditado.AdmissaoEmUtc;
    }
}