using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SisPH.Web.Filters;

/// <summary>
/// Exige que los usuarios creados con contraseña temporal actualicen su clave
/// antes de acceder a cualquier otro módulo del sistema (#14).
/// </summary>
public class ForzarCambioPasswordFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        bool debeCambiarPassword =
            httpContext.Items.TryGetValue("DebeCambiarPassword", out var valor) && valor is bool bandera
                ? bandera
                : httpContext.User.Identity?.IsAuthenticated == true &&
                  httpContext.User.HasClaim("DebeCambiarPassword", "true");

        if (httpContext.User.Identity?.IsAuthenticated == true && debeCambiarPassword)
        {
            var controlador = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
            var accion = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

            bool rutaPermitida =
                (string.Equals(controlador, "Cuenta", StringComparison.OrdinalIgnoreCase) &&
                 (string.Equals(accion, "CambiarPassword", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(accion, "Logout", StringComparison.OrdinalIgnoreCase))) ||
                string.Equals(controlador, "Error", StringComparison.OrdinalIgnoreCase);

            if (!rutaPermitida)
            {
                context.Result = new RedirectToActionResult("CambiarPassword", "Cuenta", null);
                return;
            }
        }

        await next();
    }
}
