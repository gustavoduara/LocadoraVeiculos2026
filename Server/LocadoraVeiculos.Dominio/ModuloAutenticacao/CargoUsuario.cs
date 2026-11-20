using System.ComponentModel.DataAnnotations;

namespace LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

public enum CargoUsuario
{
    Empresa,
    [Display(Name = "Funcionário")] Funcionario,

    /// <summary>Pessoa que usa o catálogo para procurar e alugar carros; não pertence a nenhuma locadora.</summary>
    Cliente
}