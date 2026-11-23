using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloCliente;

/// <summary>
/// Cliente da locadora: pessoa física (CPF) ou pessoa jurídica (CNPJ).
/// Uma pessoa física pode estar vinculada a uma pessoa jurídica (ex.: funcionário de uma empresa cliente).
/// </summary>
public class Cliente : EntidadeBase<Cliente>
{
    public TipoCliente Tipo { get; private set; }

    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;

    // Documentos: CPF, RG e CNH só existem em pessoa física; CNPJ só em pessoa jurídica.
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public string? Cnh { get; set; }
    public string? Cnpj { get; set; }

    // Endereço
    public string Estado { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;

    /// <summary>Vínculo opcional de uma pessoa física com uma pessoa jurídica.</summary>
    public Guid? PessoaJuridicaId { get; set; }
    public Cliente? PessoaJuridica { get; set; }

    /// <summary>
    /// Verdadeiro quando a ficha nasceu de uma reserva online: a pessoa tem conta própria e é ela quem cuida dos
    /// próprios dados (em "Meus dados"). A locadora só consulta; não edita.
    /// </summary>
    public bool ContaOnline { get; set; }

    protected Cliente() { }

    public static Cliente NovaPessoaFisica(
        Guid empresaId,
        string nome,
        string email,
        string telefone,
        string cpf,
        string rg,
        string cnh,
        string estado,
        string cidade,
        string bairro,
        string rua,
        string numero,
        Guid? pessoaJuridicaId
    )
    {
        return new Cliente
        {
            EmpresaId = empresaId,
            Tipo = TipoCliente.PessoaFisica,
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Cpf = cpf,
            Rg = rg,
            Cnh = cnh,
            Estado = estado,
            Cidade = cidade,
            Bairro = bairro,
            Rua = rua,
            Numero = numero,
            PessoaJuridicaId = pessoaJuridicaId
        };
    }

    public static Cliente NovaPessoaJuridica(
        Guid empresaId,
        string nome,
        string email,
        string telefone,
        string cnpj,
        string estado,
        string cidade,
        string bairro,
        string rua,
        string numero
    )
    {
        return new Cliente
        {
            EmpresaId = empresaId,
            Tipo = TipoCliente.PessoaJuridica,
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Cnpj = cnpj,
            Estado = estado,
            Cidade = cidade,
            Bairro = bairro,
            Rua = rua,
            Numero = numero
        };
    }

    /// <summary>O tipo do cliente nunca muda; os campos de documento seguem o tipo do registro existente.</summary>
    public override void AtualizarRegistro(Cliente registroEditado)
    {
        Nome = registroEditado.Nome;
        Email = registroEditado.Email;
        Telefone = registroEditado.Telefone;
        Estado = registroEditado.Estado;
        Cidade = registroEditado.Cidade;
        Bairro = registroEditado.Bairro;
        Rua = registroEditado.Rua;
        Numero = registroEditado.Numero;

        if (Tipo == TipoCliente.PessoaFisica)
        {
            Cpf = registroEditado.Cpf;
            Rg = registroEditado.Rg;
            Cnh = registroEditado.Cnh;
            PessoaJuridicaId = registroEditado.PessoaJuridicaId;
        }
        else
        {
            Cnpj = registroEditado.Cnpj;
        }
    }
}
