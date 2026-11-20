using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.ModuloAluguel;

public enum StatusAluguel
{
    [Display(Name = "Em aberto")] EmAberto,
    [Display(Name = "Concluído")] Concluido
}
