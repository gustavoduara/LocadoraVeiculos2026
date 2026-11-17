namespace LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

public interface ITenantProvider
{
    Guid? EmpresaId { get; }
    Guid? UsuarioId { get; }
    bool EstaNoCargo(string cargo);
}