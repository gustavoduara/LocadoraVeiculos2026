using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

namespace LocadoraDeVeiculos.Dominio.ModuloAluguel;

/// <summary>
/// Taxa ou serviço contratado em um aluguel. Guarda uma cópia do nome e do preço no momento da
/// contratação: alterar o catálogo depois não muda o valor de aluguéis já registrados.
/// </summary>
public class AluguelTaxa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AluguelId { get; set; }

    public Guid TaxaServicoId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public TipoCobrancaTaxa TipoCobranca { get; set; }

    protected AluguelTaxa() { }

    public AluguelTaxa(Guid taxaServicoId, string nome, decimal precoUnitario, TipoCobrancaTaxa tipoCobranca)
    {
        TaxaServicoId = taxaServicoId;
        Nome = nome;
        PrecoUnitario = precoUnitario;
        TipoCobranca = tipoCobranca;
    }

    /// <summary>Preço fixo é cobrado uma vez; cobrança diária é multiplicada pelos dias do aluguel.</summary>
    public decimal ValorPara(int dias) =>
        TipoCobranca == TipoCobrancaTaxa.Fixo ? PrecoUnitario : PrecoUnitario * dias;
}
