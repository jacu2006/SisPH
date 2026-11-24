using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

public class IncidenciasController : Controller
{
    private readonly IServicioIncidencias _incidencias;
    private readonly IWebHostEnvironment _env;

    public IncidenciasController(IServicioIncidencias incidencias, IWebHostEnvironment env)
    {
        _incidencias = incidencias;
        _env = env;
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpGet]
    public async Task<IActionResult> Index(EstadoIncidencia? estado = null)
    {
        ViewBag.EstadoSeleccionado = estado;
        var lista = await _incidencias.ListarAsync(estado, null);
        return View(lista);
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost]
    public async Task<IActionResult> Tomar(int id)
    {
        var res = await _incidencias.TomarAsync(id, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost]
    public async Task<IActionResult> Resolver(int id, string observacionTecnica)
    {
        var res = await _incidencias.ResolverAsync(id, observacionTecnica, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Operador,Residente")]
    [HttpPost]
    public async Task<IActionResult> Cerrar(int id)
    {
        // OWASP A01: Si es Residente, validar que la incidencia sea propia
        if (User.IsInRole("Residente"))
        {
            var residenteId = User.ObtenerResidenteId();
            var inc = await _incidencias.ObtenerPorIdAsync(id);
            if (inc is null)
                return NotFound();

            if (!residenteId.HasValue || inc.ResidenteId != residenteId.Value)
                return Forbid();
        }

        var res = await _incidencias.CerrarAsync(id, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return User.IsInRole("Residente")
            ? RedirectToAction(nameof(MisIncidencias))
            : RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Residente")]
    [HttpGet]
    public async Task<IActionResult> MisIncidencias()
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
        {
            TempData["MensajeError"] = "Tu cuenta no tiene un perfil de residente asociado.";
            return RedirectToAction("Panel", "Home");
        }

        var lista = await _incidencias.ListarAsync(null, residenteId.Value);
        return View(lista);
    }

    [Authorize(Roles = "Residente")]
    [HttpPost]
    public async Task<IActionResult> Reabrir(int id, string motivoReapertura)
    {
        var residenteId = User.ObtenerResidenteId();
        var inc = await _incidencias.ObtenerPorIdAsync(id);
        if (inc is null)
            return NotFound();

        // OWASP A01: Verificar propiedad del residente
        if (!residenteId.HasValue || inc.ResidenteId != residenteId.Value)
            return Forbid();

        var res = await _incidencias.ReabrirAsync(id, motivoReapertura, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(MisIncidencias));
    }

    [Authorize(Roles = "Residente")]
    [HttpGet]
    public IActionResult Reportar() => View();

    [Authorize(Roles = "Residente")]
    [HttpPost]
    public async Task<IActionResult> Reportar(string titulo, string descripcion, IFormFile? foto)
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
        {
            TempData["MensajeError"] = "Tu cuenta no tiene un perfil de residente asociado.";
            return RedirectToAction("Panel", "Home");
        }

        Stream? stream = null;
        try
        {
            ArchivoSubido? archivo = null;
            if (foto is not null && foto.Length > 0)
            {
                if (foto.Length > 2 * 1024 * 1024)
                {
                    TempData["MensajeError"] = "La fotografía no puede superar los 2 MB.";
                    ViewBag.Titulo = titulo;
                    ViewBag.Descripcion = descripcion;
                    return View();
                }

                stream = foto.OpenReadStream();
                archivo = new ArchivoSubido(stream, foto.FileName, foto.Length);
            }

            var res = await _incidencias.ReportarAsync(
                residenteId.Value,
                titulo,
                descripcion,
                archivo,
                _env.WebRootPath,
                User.ObtenerUsuarioId());

            if (!res.Exito)
            {
                TempData["MensajeError"] = res.Mensaje;
                ViewBag.Titulo = titulo;
                ViewBag.Descripcion = descripcion;
                return View();
            }

            TempData["MensajeExito"] = res.Mensaje;
            return RedirectToAction(nameof(MisIncidencias));
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }
}
