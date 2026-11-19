using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

public enum TipoPlano
{
    [Display(Name = "Plano Diário")] Diario,
    [Display(Name = "Plano Controlado")] Controlado,
    [Display(Name = "Plano Livre")] Livre
}
