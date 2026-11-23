using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.Compartilhado;

public enum TipoSeguro
{
    [Display(Name = "Sem seguro")] Nenhum,
    [Display(Name = "Com seguro")] Contratado
}
