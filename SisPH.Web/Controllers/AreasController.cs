using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AreasController : Controller
{
    private readonly IServicioAreas _areas;
    private readonly IWebHostEnvironment _env;

    public AreasController(IServicioAreas areas, IWebHostEnvironment env)
    {
        _areas = areas;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var lista = await _areas.ListarAsync(null);
        return View(lista);
    }

    [HttpGet]
    public IActionResult Crear() => View();

    [HttpPost]
    public async Task<IActionResult> Crear(
        string nombre,
        string? descripcion,
        int capacidad,
        decimal costo,
        decimal deposito,
        TimeSpan horaApertura,
        TimeSpan horaCierre,
        List<IFormFile> imagenes,
        int indicePrincipal = 0)
    {
        var streams = new List<Stream>();
        try
        {
            var archivos = new List<ArchivoSubido>();
            if (imagenes is not null)
            {
                foreach (var img in imagenes.Where(f => f.Length > 0))
                {
                    var st = img.OpenReadStream();
                    streams.Add(st);
                    archivos.Add(new ArchivoSubido(st, img.FileName, img.Length));
                }
            }

            var res = await _areas.CrearAsync(
                nombre, descripcion, capacidad, costo, deposito,
                horaApertura, horaCierre, archivos, indicePrincipal,
                _env.WebRootPath, User.ObtenerUsuarioId());

            if (!res.Exito)
            {
                TempData["MensajeError"] = res.Mensaje;
                return View();
            }

            TempData["MensajeExito"] = res.Mensaje;
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            foreach (var st in streams)
                await st.DisposeAsync();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var area = await _areas.ObtenerPorIdAsync(id);
        if (area is null)
            return NotFound();

        return View(area);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var area = await _areas.ObtenerPorIdAsync(id);
        if (area is null)
            return NotFound();

        return View(area);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(
        int id,
        string nombre,
        string? descripcion,
        int capacidad,
        decimal costo,
        decimal deposito,
        TimeSpan horaApertura,
        TimeSpan horaCierre,
        List<IFormFile>? nuevasImagenes,
        int? imagenPrincipalId)
    {
        var streams = new List<Stream>();
        try
        {
            var archivos = new List<ArchivoSubido>();
            if (nuevasImagenes is not null)
            {
                foreach (var img in nuevasImagenes.Where(f => f.Length > 0))
                {
                    var st = img.OpenReadStream();
                    streams.Add(st);
                    archivos.Add(new ArchivoSubido(st, img.FileName, img.Length));
                }
            }

            var res = await _areas.ModificarAsync(
                id, nombre, descripcion, capacidad, costo, deposito,
                horaApertura, horaCierre, archivos, imagenPrincipalId,
                _env.WebRootPath, User.ObtenerUsuarioId());

            if (!res.Exito)
            {
                TempData["MensajeError"] = res.Mensaje;
                var area = await _areas.ObtenerPorIdAsync(id);
                return View(area);
            }

            TempData["MensajeExito"] = res.Mensaje;
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            foreach (var st in streams)
                await st.DisposeAsync();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var res = await _areas.CambiarEstadoAsync(id, activo, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }
}