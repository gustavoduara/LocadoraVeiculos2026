using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

public enum TipoCobrancaTaxa
{
    [Display(Name = "Preço fixo")] Fixo,
    [Display(Name = "Cobrança diária")] Diario
}
