using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

public class ReservasController : Controller
{
    private readonly IServicioReservas _reservas;
    private readonly IServicioAreas _areas;
    private readonly IServicioQR _qr;

    public ReservasController(IServicioReservas reservas, IServicioAreas areas, IServicioQR qr)
    {
        _reservas = reservas;
        _areas = areas;
        _qr = qr;
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Index(EstadoReserva? estado = null)
    {
        ViewBag.EstadoSeleccionado = estado;
        var lista = await _reservas.ListarAsync(estado, null);
        return View(lista);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Aprobar(int id)
    {
        var urlBase = Url.Action(nameof(Index), "Reservas", null, Request.Scheme) ?? string.Empty;
        var res = await _reservas.AprobarAsync(id, User.ObtenerUsuarioId(), urlBase);
        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        TempData["MensajeExito"] = res.Mensaje;
        TempData["QrReservaAprobada"] = res.Datos;
        TempData["ReservaAprobadaId"] = id;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Rechazar(int id, string motivo)
    {
        var res = await _reservas.RechazarAsync(id, motivo, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Residente")]
    [HttpGet]
    public async Task<IActionResult> MisReservas()
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
        {
            TempData["MensajeError"] = "Tu cuenta no tiene un perfil de residente asociado.";
            return RedirectToAction("Panel", "Home");
        }

        var lista = await _reservas.ListarAsync(null, residenteId.Value);
        var urlBase = Url.Action(nameof(MisReservas), "Reservas", null, Request.Scheme) ?? string.Empty;
        var qrs = lista
            .Where(r => r.Estado == EstadoReserva.Aprobada)
            .ToDictionary(r => r.Id, r => _qr.GenerarPngBase64($"{urlBase.TrimEnd('/')}/{r.Id}"));
        ViewBag.QrsAprobadas = qrs;

        return View(lista);
    }

    [Authorize(Roles = "Residente")]
    [HttpPost]
    public async Task<IActionResult> Cancelar(int id)
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
            return Forbid();

        // OWASP A01: Verificar que la reserva pertenezca al residente autenticado
        var reserva = await _reservas.ObtenerPorIdAsync(id);
        if (reserva is null)
            return NotFound();

        if (reserva.ResidenteId != residenteId.Value)
            return Forbid();

        var res = await _reservas.CancelarAsync(id, User.ObtenerUsuarioId());
        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(MisReservas));
    }

    [Authorize(Roles = "Residente")]
    [HttpGet]
    public async Task<IActionResult> Solicitar(int? areaId = null, DateTime? fecha = null)
    {
        var areas = await _areas.ListarAsync(true);
        var fechaConsulta = (fecha ?? DateTime.Today.AddDays(1)).Date;
        var seleccionada = areaId.HasValue
            ? areas.FirstOrDefault(a => a.Id == areaId.Value)
            : areas.FirstOrDefault();

        var modelo = new SolicitarReservaViewModel
        {
            Areas = areas,
            AreaId = seleccionada?.Id,
            AreaSeleccionada = seleccionada,
            Fecha = fechaConsulta,
            HoraInicio = seleccionada?.HoraApertura ?? new TimeSpan(10, 0, 0),
            HoraFin = seleccionada is not null && seleccionada.HoraApertura.Add(TimeSpan.FromHours(2)) <= seleccionada.HoraCierre
                ? seleccionada.HoraApertura.Add(TimeSpan.FromHours(2))
                : seleccionada?.HoraCierre ?? new TimeSpan(14, 0, 0)
        };

        if (seleccionada is not null)
        {
            modelo.FranjasOcupadas = await _reservas.ObtenerFranjasOcupadasAsync(seleccionada.Id, fechaConsulta);
        }

        return View(modelo);
    }

    [Authorize(Roles = "Residente")]
    [HttpPost]
    public async Task<IActionResult> Solicitar(int areaId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin)
    {
        var residenteId = User.ObtenerResidenteId();
        if (!residenteId.HasValue)
        {
            TempData["MensajeError"] = "Tu cuenta no tiene un perfil de residente asociado.";
            return RedirectToAction("Panel", "Home");
        }

        var res = await _reservas.SolicitarAsync(
            areaId,
            residenteId.Value,
            fecha,
            horaInicio,
            horaFin,
            User.ObtenerUsuarioId());

        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            var areas = await _areas.ListarAsync(true);
            var seleccionada = areas.FirstOrDefault(a => a.Id == areaId);
            var modelo = new SolicitarReservaViewModel
            {
                Areas = areas,
                AreaId = areaId,
                AreaSeleccionada = seleccionada,
                Fecha = fecha.Date,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                FranjasOcupadas = await _reservas.ObtenerFranjasOcupadasAsync(areaId, fecha.Date)
            };
            return View(modelo);
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction(nameof(MisReservas));
    }
}
