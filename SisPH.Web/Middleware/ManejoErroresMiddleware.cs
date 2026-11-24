using System.Security.Claims;
using SisPH.Datos.Interfaces;

namespace SisPH.Web.Middleware;

public class ManejoErroresMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ManejoErroresMiddleware> _logger;

    public ManejoErroresMiddleware(RequestDelegate next, ILogger<ManejoErroresMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx, IRepositorioLogError repo)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", ctx.Request.Path);

            int? usuarioId = null;
            var claimId = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claimId, out int parsedId))
                usuarioId = parsedId;

            var origen = ex.TargetSite?.DeclaringType?.FullName ?? "SisPH.Web";
            var ruta = $"{ctx.Request.Method} {ctx.Request.Path}{ctx.Request.QueryString}";
            var ip = ctx.Connection.RemoteIpAddress?.ToString();

            long refId = 0;
            try
            {
                refId = await repo.GuardarAsync(ex, origen, ruta, ip, usuarioId);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "No se pudo guardar la excepción en la tabla LogError.");
            }

            if (!ctx.Response.HasStarted)
            {
                ctx.Response.Clear();
                ctx.Response.Redirect($"/Error?refId={refId}");
            }
        }
    }
}