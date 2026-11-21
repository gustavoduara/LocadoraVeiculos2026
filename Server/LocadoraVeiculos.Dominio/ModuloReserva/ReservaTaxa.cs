using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

namespace LocadoraDeVeiculos.Dominio.ModuloReserva;

/// <summary>
/// Taxa ou serviço que o cliente escolheu ao pedir a reserva. Guarda uma cópia do nome e do preço do momento do pedido
/// (o catálogo da locadora pode mudar depois). Sem vínculo com a taxa original: excluir a taxa do catálogo não trava nada.
/// </summary>
public class ReservaTaxa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReservaId { get; set; }

    /// <summary>A taxa do catálogo da locadora: usada para criar o aluguel quando o pedido é aprovado.</summary>
    public Guid TaxaServicoId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public TipoCobrancaTaxa TipoCobranca { get; set; }

    public ReservaTaxa() { }
}
