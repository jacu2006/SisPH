using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class CuotasController : Controller
{
    private readonly IServicioCuotas _cuotas;
    private readonly IServicioUnidades _unidades;

    public CuotasController(IServicioCuotas cuotas, IServicioUnidades unidades)
    {
        _cuotas = cuotas;
        _unidades = unidades;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? periodo = null, int? torreId = null, EstadoCuota? estado = null)
    {
        ViewBag.Torres = await _unidades.ListarTorresAsync();
        ViewBag.PeriodoActual = periodo;
        ViewBag.TorreSeleccionada = torreId;
        ViewBag.EstadoSeleccionado = estado;

        var lista = await _cuotas.ListarPorPeriodoAsync(periodo, torreId, estado);
        return View(lista);
    }

    [HttpPost]
    public async Task<IActionResult> ActualizarEstados()
    {
        var res = await _cuotas.ActualizarEstadosYRecargosAsync(10m, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Generar()
    {
        var modelo = new CuotasGenerarViewModel
        {
            Periodo = DateTime.Today.ToString("yyyy-MM"),
            Tipo = TipoCuota.Ordinaria
        };
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Generar(CuotasGenerarViewModel modelo, string accion = "previa")
    {
        if (accion == "confirmar")
        {
            var resGen = await _cuotas.GenerarCuotasAsync(
                modelo.Periodo,
                modelo.Presupuesto,
                modelo.Tipo,
                modelo.FechaVencimiento,
                User.ObtenerUsuarioId());

            if (!resGen.Exito)
            {
                TempData["MensajeError"] = resGen.Mensaje;
                var previaError = await _cuotas.GenerarVistaPreviaAsync(modelo.Periodo, modelo.Presupuesto, modelo.Tipo);
                if (previaError.Exito)
                    modelo.VistaPrevia = previaError.Datos;
                return View(modelo);
            }

            TempData["MensajeExito"] = resGen.Mensaje;
            return RedirectToAction(nameof(Index), new { periodo = modelo.Periodo });
        }

        var previa = await _cuotas.GenerarVistaPreviaAsync(modelo.Periodo, modelo.Presupuesto, modelo.Tipo);
        if (!previa.Exito)
        {
            TempData["MensajeError"] = previa.Mensaje;
            return View(modelo);
        }

        modelo.VistaPrevia = previa.Datos;
        return View(modelo);
    }
}
