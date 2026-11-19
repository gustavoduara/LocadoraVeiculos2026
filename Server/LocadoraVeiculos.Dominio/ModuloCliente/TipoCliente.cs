using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.ModuloCliente;

public enum TipoCliente
{
    [Display(Name = "Pessoa Física")] PessoaFisica,
    [Display(Name = "Pessoa Jurídica")] PessoaJuridica
}
