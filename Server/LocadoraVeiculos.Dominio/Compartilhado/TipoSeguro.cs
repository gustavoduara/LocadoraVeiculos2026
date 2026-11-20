using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.Compartilhado;

public enum TipoSeguro
{
    [Display(Name = "Sem seguro")] Nenhum,
    [Display(Name = "Seguro para o cliente")] Cliente,
    [Display(Name = "Seguro para terceiros")] Terceiros
}
