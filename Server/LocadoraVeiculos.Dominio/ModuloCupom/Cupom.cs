using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloParceiro;

namespace LocadoraDeVeiculos.Dominio.ModuloCupom;

/// <summary>
/// Cupom de desconto (valor fixo em reais ou porcentagem), emitido por um parceiro e com data de validade.
/// Só cupons ativos (não expirados) podem ser usados em um aluguel, e cada cliente usa o mesmo cupom uma única vez.
/// </summary>
public class Cupom : EntidadeBase<Cupom>
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>Valor fixo em reais ou porcentagem (de 0 a 100), conforme o <see cref="Tipo"/>.</summary>
    public decimal Valor { get; set; }

    public TipoDesconto Tipo { get; set; } = TipoDesconto.Valor;

    /// <summary>Último dia em que o cupom pode ser usado (inclusive).</summary>
    public DateOnly DataValidade { get; set; }

    public Guid ParceiroId { get; set; }
    public Parceiro? Parceiro { get; set; }

    protected Cupom() { }

    public Cupom(Guid empresaId, string nome, decimal valor, DateOnly dataValidade, Guid parceiroId, TipoDesconto tipo = TipoDesconto.Valor)
    {
        EmpresaId = empresaId;
        Nome = nome;
        Valor = valor;
        Tipo = tipo;
        DataValidade = dataValidade;
        ParceiroId = parceiroId;
    }

    public bool EstaAtivo(DateOnly hoje) => DataValidade >= hoje;

    public override void AtualizarRegistro(Cupom registroEditado)
    {
        Nome = registroEditado.Nome;
        Valor = registroEditado.Valor;
        Tipo = registroEditado.Tipo;
        DataValidade = registroEditado.DataValidade;
        ParceiroId = registroEditado.ParceiroId;
    }
}
