using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ResidentesController : Controller
{
    private readonly IServicioResidentes _residentes;
    private readonly IServicioUnidades _unidades;
    private readonly IWebHostEnvironment _env;

    public ResidentesController(
        IServicioResidentes residentes,
        IServicioUnidades unidades,
        IWebHostEnvironment env)
    {
        _residentes = residentes;
        _unidades = unidades;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string filtro = "activos")
    {
        bool? soloActivos = filtro == "activos" ? true : filtro == "inactivos" ? false : null;
        ViewBag.FiltroActual = filtro;
        var lista = await _residentes.ListarAsync(soloActivos);
        return View(lista);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        ViewBag.Unidades = await _unidades.ListarUnidadesAsync(null, true);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        string cedula,
        string nombre,
        string apellido,
        string? telefono,
        string correo,
        int unidadId,
        TipoOcupacion tipoOcupacion,
        bool crearUsuarioPortal,
        IFormFile? foto)
    {
        Stream? stream = null;
        try
        {
            ArchivoSubido? archivo = null;
            if (foto is not null && foto.Length > 0)
            {
                stream = foto.OpenReadStream();
                archivo = new ArchivoSubido(stream, foto.FileName, foto.Length);
            }

            var res = await _residentes.CrearAsync(
                cedula, nombre, apellido, telefono, correo,
                unidadId, tipoOcupacion, crearUsuarioPortal,
                archivo, _env.WebRootPath, User.ObtenerUsuarioId());

            if (!res.Exito)
            {
                TempData["MensajeError"] = res.Mensaje;
                ViewBag.Unidades = await _unidades.ListarUnidadesAsync(null, true);
                return View();
            }

            TempData["MensajeExito"] = res.Mensaje;
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var residente = await _residentes.ObtenerPorIdAsync(id);
        if (residente is null)
            return NotFound();

        return View(residente);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(
        int id,
        string cedula,
        string nombre,
        string apellido,
        string? telefono,
        string correo,
        IFormFile? nuevaFoto)
    {
        Stream? stream = null;
        try
        {
            ArchivoSubido? archivo = null;
            if (nuevaFoto is not null && nuevaFoto.Length > 0)
            {
                stream = nuevaFoto.OpenReadStream();
                archivo = new ArchivoSubido(stream, nuevaFoto.FileName, nuevaFoto.Length);
            }

            var res = await _residentes.ModificarAsync(
                id, cedula, nombre, apellido, telefono, correo,
                archivo, _env.WebRootPath, User.ObtenerUsuarioId());

            if (!res.Exito)
            {
                TempData["MensajeError"] = res.Mensaje;
                var residente = await _residentes.ObtenerPorIdAsync(id);
                return View(residente);
            }

            TempData["MensajeExito"] = res.Mensaje;
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var residente = await _residentes.ObtenerPorIdAsync(id);
        if (residente is null)
            return NotFound();

        return View(residente);
    }

    [HttpPost]
    public async Task<IActionResult> DarDeBaja(int id)
    {
        var res = await _residentes.DarDeBajaAsync(id, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }
}