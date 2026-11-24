using System.Security.Claims;

namespace SisPH.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int ObtenerUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, out int id) ? id : 0;
    }

    public static int? ObtenerResidenteId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue("ResidenteId");
        return int.TryParse(valor, out int id) ? id : null;
    }
}