namespace LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

/// <summary>O lado do sistema por onde a pessoa diz que quer entrar (as abas da tela de login).</summary>
public enum TipoDeAcesso
{
    /// <summary>Quem procura e aluga carros: conta de Cliente.</summary>
    Cliente,

    /// <summary>Quem trabalha numa locadora: conta de Empresa ou de Funcionário.</summary>
    Locadora
}
