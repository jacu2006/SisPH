using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
public class MudanzasController : Controller
{
    private readonly IServicioMudanzas _mudanzas;
    private readonly IServicioUnidades _unidades;
    private readonly IServicioResidentes _residentes;

    public MudanzasController(
        IServicioMudanzas mudanzas,
        IServicioUnidades unidades,
        IServicioResidentes residentes)
    {
        _mudanzas = mudanzas;
        _unidades = unidades;
        _residentes = residentes;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? unidadId = null)
    {
        ViewBag.Unidades = await _unidades.ListarUnidadesAsync(null, true);
        ViewBag.UnidadSeleccionada = unidadId;
        var lista = await _mudanzas.ListarAsync(unidadId);
        return View(lista);
    }

    [HttpGet]
    public async Task<IActionResult> Registrar()
    {
        var modelo = new RegistrarMudanzaViewModel
        {
            Unidades = await _unidades.ListarUnidadesAsync(null, true),
            Residentes = await _residentes.ListarAsync(true),
            Tipo = TipoMudanza.Entrada,
            TipoOcupacionEntrada = TipoOcupacion.Propietario
        };
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(
        int unidadId,
        TipoMudanza tipo,
        int? residenteIdEntrada,
        TipoOcupacion tipoOcupacionEntrada,
        string? observacion)
    {
        var res = await _mudanzas.RegistrarMudanzaAsync(
            unidadId,
            tipo,
            residenteIdEntrada,
            tipoOcupacionEntrada,
            observacion,
            User.ObtenerUsuarioId());

        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            var modelo = new RegistrarMudanzaViewModel
            {
                Unidades = await _unidades.ListarUnidadesAsync(null, true),
                Residentes = await _residentes.ListarAsync(true),
                UnidadId = unidadId,
                Tipo = tipo,
                ResidenteIdEntrada = residenteIdEntrada,
                TipoOcupacionEntrada = tipoOcupacionEntrada,
                Observacion = observacion
            };
            return View(modelo);
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }
}
