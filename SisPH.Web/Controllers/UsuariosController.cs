using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IServicioUsuarios _usuarios;

    public UsuariosController(IServicioUsuarios usuarios)
    {
        _usuarios = usuarios;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string filtro = "activos")
    {
        bool? soloActivos = filtro switch
        {
            "activos" => true,
            "inactivos" => false,
            _ => null
        };

        ViewBag.FiltroActual = filtro;
        var lista = await _usuarios.ListarAsync(soloActivos);
        return View(lista);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        ViewBag.Roles = await _usuarios.ListarRolesAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Crear(string nombre, string correo, int rolId)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || rolId <= 0)
        {
            TempData["MensajeError"] = "Todos los campos son obligatorios.";
            ViewBag.Roles = await _usuarios.ListarRolesAsync();
            return View();
        }

        var res = await _usuarios.CrearAsync(nombre, correo, rolId, User.ObtenerUsuarioId());
        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            ViewBag.Roles = await _usuarios.ListarRolesAsync();
            return View();
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id);
        if (usuario is null)
            return NotFound();

        ViewBag.Roles = await _usuarios.ListarRolesAsync();
        return View(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, string nombre, string correo, int rolId)
    {
        var res = await _usuarios.ModificarAsync(id, nombre, correo, rolId, User.ObtenerUsuarioId());
        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            var usuario = await _usuarios.ObtenerPorIdAsync(id);
            ViewBag.Roles = await _usuarios.ListarRolesAsync();
            return View(usuario);
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var res = await _usuarios.CambiarEstadoActivoAsync(id, activo, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }
}