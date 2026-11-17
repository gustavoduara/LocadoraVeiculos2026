using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;

namespace LocadoraDeVeiculos.WebApi.Config.Identity;

public class IdentityTenantProvider(IHttpContextAccessor contextAccessor) : ITenantProvider
{
    public Guid? EmpresaId
    {
        get
        {
            var user = contextAccessor.HttpContext?.User;

            var empresaIdClaim = user?.FindFirst("EmpresaId")?.Value;

            if (Guid.TryParse(empresaIdClaim, out var empresaId))
                return empresaId;

            return null;
        }
    }

    public Guid? UsuarioId
    {
        get
        {
            var user = contextAccessor.HttpContext?.User;

            var usuarioIdClaim = user?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                                 ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(usuarioIdClaim, out var usuarioId))
                return usuarioId;

            return null;
        }
    }

    public bool EstaNoCargo(string cargo)
    {
        return contextAccessor.HttpContext?.User?.IsInRole(cargo) ?? false;
    }
}