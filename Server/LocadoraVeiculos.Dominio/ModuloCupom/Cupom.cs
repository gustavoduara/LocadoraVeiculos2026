using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloParceiro;

namespace LocadoraDeVeiculos.Dominio.ModuloCupom;

/// <summary>
/// Cupom de desconto de valor fixo, emitido por um parceiro e com data de validade.
/// Só cupons ativos (não expirados) podem ser usados em um aluguel, e cada cliente usa o mesmo cupom uma única vez.
/// </summary>
public class Cupom : EntidadeBase<Cupom>
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>Desconto fixo, em reais, abatido do valor da locação.</summary>
    public decimal Valor { get; set; }

    /// <summary>Último dia em que o cupom pode ser usado (inclusive).</summary>
    public DateOnly DataValidade { get; set; }

    public Guid ParceiroId { get; set; }
    public Parceiro? Parceiro { get; set; }

    protected Cupom() { }

    public Cupom(Guid empresaId, string nome, decimal valor, DateOnly dataValidade, Guid parceiroId)
    {
        EmpresaId = empresaId;
        Nome = nome;
        Valor = valor;
        DataValidade = dataValidade;
        ParceiroId = parceiroId;
    }

    public bool EstaAtivo(DateOnly hoje) => DataValidade >= hoje;

    public override void AtualizarRegistro(Cupom registroEditado)
    {
        Nome = registroEditado.Nome;
        Valor = registroEditado.Valor;
        DataValidade = registroEditado.DataValidade;
        ParceiroId = registroEditado.ParceiroId;
    }
}
