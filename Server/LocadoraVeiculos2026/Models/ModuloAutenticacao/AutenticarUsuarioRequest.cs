namespace LocadoraDeVeiculos.WebApi.Models.ModuloAutenticacao;

using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

/// <summary><c>Tipo</c> é opcional: quando informado (Cliente ou Locadora), a conta precisa ser desse lado.</summary>
public record AutenticarUsuarioRequest(string Email, string Senha, TipoDeAcesso? Tipo = null);