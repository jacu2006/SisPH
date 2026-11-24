using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

public class PagosController : Controller
{
    private readonly IServicioPagos _pagos;
    private readonly IServicioCuotas _cuotas;
    private readonly IServicioUnidades _unidades;
    private readonly IServicioCuenta _cuenta;

    public PagosController(
        IServicioPagos pagos,
        IServicioCuotas cuotas,
        IServicioUnidades unidades,
        IServicioCuenta cuenta)
    {
        _pagos = pagos;
        _cuotas = cuotas;
        _unidades = unidades;
        _cuenta = cuenta;
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpGet]
    public async Task<IActionResult> Index(int? unidadId = null)
    {
        ViewBag.Unidades = await _unidades.ListarUnidadesAsync(null, true);
        ViewBag.UnidadSeleccionada = unidadId;
        var lista = await _pagos.ListarRecientesAsync(unidadId);
        return View(lista);
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpGet]
    public async Task<IActionResult> Registrar(int? unidadId = null)
    {
        var modelo = new RegistrarPagoViewModel
        {
            Unidades = await _unidades.ListarUnidadesAsync(null, true),
            UnidadId = unidadId
        };

        if (unidadId.HasValue)
        {
            modelo.CuotasPendientes = await _cuotas.ListarPendientesPorUnidadAsync(unidadId.Value);
        }

        return View(modelo);
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost]
    public async Task<IActionResult> Registrar(
        int unidadId,
        List<int>? cuotaIds,
        decimal montoPagado,
        MetodoPago metodo,
        string? referencia)
    {
        var urlBase = Url.Action(nameof(Verificar), "Pagos", null, Request.Scheme) ?? string.Empty;

        var res = await _pagos.RegistrarPagoAsync(
            unidadId,
            cuotaIds ?? new List<int>(),
            montoPagado,
            metodo,
            referencia,
            urlBase,
            User.ObtenerUsuarioId());

        if (!res.Exito || res.Datos is null)
        {
            TempData["MensajeError"] = res.Mensaje;
            var modelo = new RegistrarPagoViewModel
            {
                Unidades = await _unidades.ListarUnidadesAsync(null, true),
                UnidadId = unidadId,
                CuotasPendientes = await _cuotas.ListarPendientesPorUnidadAsync(unidadId),
                CuotaIdsSeleccionadas = cuotaIds ?? new List<int>(),
                MontoPagado = montoPagado,
                Metodo = metodo,
                Referencia = referencia
            };
            return View(modelo);
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(Recibo), new { id = res.Datos.Pago.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Recibo(int id)
    {
        var urlBase = Url.Action(nameof(Verificar), "Pagos", null, Request.Scheme) ?? string.Empty;
        var recibo = await _pagos.ObtenerReciboVerificadoAsync(id, urlBase);
        if (recibo is null)
            return NotFound();

        // OWASP A01: Validar que un Residente solo acceda a recibos de su propia unidad
        if (User.IsInRole("Residente"))
        {
            var portal = await _cuenta.ObtenerPortalResidenteAsync(User.ObtenerUsuarioId());
            bool esPropio = portal is not null &&
                (portal.UnidadVigente?.Id == recibo.Pago.UnidadId ||
                 portal.Pagos.Any(p => p.Id == id));

            if (!esPropio)
                return Forbid();
        }

        return View(recibo);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Verificar(int id)
    {
        var urlBase = Url.Action(nameof(Verificar), "Pagos", null, Request.Scheme) ?? string.Empty;
        var recibo = await _pagos.ObtenerReciboVerificadoAsync(id, urlBase);
        return View(recibo);
    }
}
