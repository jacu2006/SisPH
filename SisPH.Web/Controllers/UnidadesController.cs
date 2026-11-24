using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UnidadesController : Controller
{
    private readonly IServicioUnidades _unidades;

    public UnidadesController(IServicioUnidades unidades)
    {
        _unidades = unidades;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? torreId = null)
    {
        ViewBag.Torres = await _unidades.ListarTorresAsync();
        ViewBag.TorreSeleccionada = torreId;
        var lista = await _unidades.ListarUnidadesAsync(torreId, null);
        return View(lista);
    }

    [HttpPost]
    public async Task<IActionResult> CrearTorre(string nombre, int pisos)
    {
        var res = await _unidades.CrearTorreAsync(nombre, pisos, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CrearUnidad()
    {
        ViewBag.Torres = await _unidades.ListarTorresAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CrearUnidad(int torreId, string numero, int piso, decimal metros2, decimal coeficiente)
    {
        var res = await _unidades.CrearUnidadAsync(torreId, numero, piso, metros2, coeficiente, User.ObtenerUsuarioId());
        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            ViewBag.Torres = await _unidades.ListarTorresAsync();
            return View();
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditarUnidad(int id)
    {
        var unidad = await _unidades.ObtenerUnidadPorIdAsync(id);
        if (unidad is null)
            return NotFound();

        ViewBag.Torres = await _unidades.ListarTorresAsync();
        return View(unidad);
    }

    [HttpPost]
    public async Task<IActionResult> EditarUnidad(int id, int torreId, string numero, int piso, decimal metros2, decimal coeficiente)
    {
        var res = await _unidades.ModificarUnidadAsync(id, torreId, numero, piso, metros2, coeficiente, User.ObtenerUsuarioId());
        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            ViewBag.Torres = await _unidades.ListarTorresAsync();
            var unidad = await _unidades.ObtenerUnidadPorIdAsync(id);
            return View(unidad);
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var res = await _unidades.CambiarEstadoUnidadAsync(id, activo, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }
}