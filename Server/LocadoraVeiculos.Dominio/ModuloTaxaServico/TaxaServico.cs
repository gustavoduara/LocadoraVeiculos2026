using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

/// <summary>
/// Taxa ou serviço que o cliente pode adicionar ao aluguel (ex.: GPS, cadeirinha de bebê, tanque cheio).
/// O preço é cobrado uma única vez (fixo) ou multiplicado pelos dias do aluguel (diário).
/// </summary>
public class TaxaServico : EntidadeBase<TaxaServico>
{
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public TipoCobrancaTaxa TipoCobranca { get; set; }

    protected TaxaServico() { }

    public TaxaServico(Guid empresaId, string nome, decimal preco, TipoCobrancaTaxa tipoCobranca)
    {
        EmpresaId = empresaId;
        Nome = nome;
        Preco = preco;
        TipoCobranca = tipoCobranca;
    }

    public override void AtualizarRegistro(TaxaServico registroEditado)
    {
        Nome = registroEditado.Nome;
        Preco = registroEditado.Preco;
        TipoCobranca = registroEditado.TipoCobranca;
    }
}
