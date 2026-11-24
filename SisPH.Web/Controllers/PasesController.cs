using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Residente")]
public class PasesController : Controller
{
    private readonly IServicioPases _pases;
    private readonly IServicioQR _qr;

    public PasesController(IServicioPases pases, IServicioQR qr)
    {
        _pases = pases;
        _qr = qr;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
        {
            TempData["MensajeError"] = "Tu cuenta no tiene un perfil de residente asociado.";
            return RedirectToAction("Panel", "Home");
        }

        var lista = await _pases.ListarPorResidenteAsync(residenteId.Value);
        ViewBag.Qrs = lista.ToDictionary(p => p.Id, p => _qr.GenerarPngBase64(p.TokenQR));
        return View(lista);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ViewBag.VigenteHastaDefault = DateTime.Now.AddHours(4).ToString("yyyy-MM-ddTHH:mm");
        return View((PaseGeneradoDto?)null);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(string nombreVisitante, string? cedulaVisitante, DateTime vigenteHasta)
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
        {
            TempData["MensajeError"] = "Tu cuenta no tiene un perfil de residente asociado.";
            return RedirectToAction("Panel", "Home");
        }

        var res = await _pases.GenerarPaseAsync(
            residenteId.Value,
            nombreVisitante,
            cedulaVisitante,
            vigenteHasta,
            User.ObtenerUsuarioId());

        if (!res.Exito || res.Datos is null)
        {
            TempData["MensajeError"] = res.Mensaje;
            ViewBag.NombreVisitante = nombreVisitante;
            ViewBag.CedulaVisitante = cedulaVisitante;
            ViewBag.VigenteHastaDefault = vigenteHasta == default
                ? DateTime.Now.AddHours(4).ToString("yyyy-MM-ddTHH:mm")
                : vigenteHasta.ToString("yyyy-MM-ddTHH:mm");
            return View((PaseGeneradoDto?)null);
        }

        TempData["MensajeExito"] = res.Mensaje;
        return View(res.Datos);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarQr(int id)
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
            return Forbid();

        // OWASP A01: Verificar que el pase pertenezca al residente autenticado
        var pases = await _pases.ListarPorResidenteAsync(residenteId.Value);
        var pase = pases.FirstOrDefault(p => p.Id == id);
        if (pase is null)
            return NotFound();

        var png = _qr.GenerarPngBytes(pase.TokenQR);
        return File(png, "image/png", $"pase-visita-{pase.Id}.png");
    }
}
