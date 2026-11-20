using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloParceiro;

/// <summary>Parceiro da locadora (empresa ou pessoa que divulga cupons). Tem apenas o nome.</summary>
public class Parceiro : EntidadeBase<Parceiro>
{
    public string Nome { get; set; } = string.Empty;

    protected Parceiro() { }

    public Parceiro(Guid empresaId, string nome)
    {
        EmpresaId = empresaId;
        Nome = nome;
    }

    public override void AtualizarRegistro(Parceiro registroEditado)
    {
        Nome = registroEditado.Nome;
    }
}
