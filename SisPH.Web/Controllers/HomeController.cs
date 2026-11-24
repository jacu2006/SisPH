using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

public class HomeController : Controller
{
    private readonly IServicioUnidades _unidades;
    private readonly IServicioReportes _reportes;
    private readonly IServicioReservas _reservas;
    private readonly IServicioPases _pases;
    private readonly IServicioIncidencias _incidencias;
    private readonly IServicioCuenta _cuenta;

    public HomeController(
        IServicioUnidades unidades,
        IServicioReportes reportes,
        IServicioReservas reservas,
        IServicioPases pases,
        IServicioIncidencias incidencias,
        IServicioCuenta cuenta)
    {
        _unidades = unidades;
        _reportes = reportes;
        _reservas = reservas;
        _pases = pases;
        _incidencias = incidencias;
        _cuenta = cuenta;
    }

    [AllowAnonymous]
    public IActionResult Index() => View();

    [Authorize]
    public async Task<IActionResult> Panel()
    {
        var modelo = new PanelViewModel();

        if (User.IsInRole("Administrador"))
        {
            var unidadesActivas = await _unidades.ListarUnidadesAsync(null, true);
            var morosidad = await _reportes.ObtenerMorosidadAsync();
            var reservasPendientes = await _reservas.ListarAsync(EstadoReserva.Solicitada);
            var incidencias = await _incidencias.ListarAsync();

            modelo.UnidadesActivas = unidadesActivas.Count;
            modelo.UnidadesMorosas = morosidad.Count;
            modelo.TotalMorosidad = morosidad.Sum(m => m.SaldoAdeudado);
            modelo.ReservasPendientes = reservasPendientes.Count;
            modelo.IncidenciasAbiertas = incidencias.Count(i =>
                i.Estado == EstadoIncidencia.Abierta || i.Estado == EstadoIncidencia.EnProceso);
        }

        if (User.IsInRole("Operador"))
        {
            var historialHoy = await _pases.ListarHistorialGaritaAsync(DateTime.Today);
            var incidencias = await _incidencias.ListarAsync();

            modelo.AccesosHoy = historialHoy.Count(p => p.Entrada.HasValue);
            modelo.VisitantesEnSitio = historialHoy.Count(p => p.Entrada.HasValue && !p.Salida.HasValue);
            modelo.IncidenciasAbiertas = incidencias.Count(i =>
                i.Estado == EstadoIncidencia.Abierta || i.Estado == EstadoIncidencia.EnProceso);
        }

        if (User.IsInRole("Residente"))
        {
            modelo.PortalResidente = await _cuenta.ObtenerPortalResidenteAsync(User.ObtenerUsuarioId());
        }

        return View(modelo);
    }
}
