using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.Compartilhado;

public enum TipoCombustivel
{
    Gasolina,
    [Display(Name = "Gás")] Gas,
    Diesel,
    [Display(Name = "Álcool")] Alcool
}
