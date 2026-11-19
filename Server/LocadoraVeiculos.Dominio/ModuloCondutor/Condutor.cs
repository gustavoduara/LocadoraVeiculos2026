using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloCliente;

namespace LocadoraDeVeiculos.Dominio.ModuloCondutor;

/// <summary>
/// Pessoa autorizada a dirigir o veículo alugado por um cliente. Uma pessoa jurídica precisa de
/// ao menos um condutor (pessoa física) para realizar um aluguel; uma pessoa física pode ser o
/// próprio condutor.
/// </summary>
public class Condutor : EntidadeBase<Condutor>
{
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Cnh { get; set; } = string.Empty;
    public DateOnly ValidadeCnh { get; set; }

    protected Condutor() { }

    public Condutor(
        Guid empresaId,
        Guid clienteId,
        string nome,
        string email,
        string telefone,
        string cpf,
        string cnh,
        DateOnly validadeCnh
    )
    {
        EmpresaId = empresaId;
        ClienteId = clienteId;
        Nome = nome;
        Email = email;
        Telefone = telefone;
        Cpf = cpf;
        Cnh = cnh;
        ValidadeCnh = validadeCnh;
    }

    /// <summary>Uma CNH vencida impede o aluguel (a validação acontece no módulo de aluguéis).</summary>
    public bool CnhVencida(DateOnly hoje) => ValidadeCnh < hoje;

    /// <summary>Atualiza os dados do condutor; o cliente a quem ele pertence não muda.</summary>
    public override void AtualizarRegistro(Condutor registroEditado)
    {
        Nome = registroEditado.Nome;
        Email = registroEditado.Email;
        Telefone = registroEditado.Telefone;
        Cpf = registroEditado.Cpf;
        Cnh = registroEditado.Cnh;
        ValidadeCnh = registroEditado.ValidadeCnh;
    }
}
