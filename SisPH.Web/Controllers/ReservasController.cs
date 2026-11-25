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

    public ReservasController(IServicioReservas reservas, IServicioAreas areas)
    {
        _reservas = reservas;
        _areas = areas;
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
        // [#11] QR unificado apuntando a Reservas/Verificar/{id}?f={firmaHmac}
        var urlBase = Url.Action(nameof(Verificar), "Reservas", null, Request.Scheme) ?? string.Empty;
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
        var urlBase = Url.Action(nameof(Verificar), "Reservas", null, Request.Scheme) ?? string.Empty;
        var qrs = lista
            .Where(r => r.Estado == EstadoReserva.Aprobada)
            .ToDictionary(r => r.Id, r => _reservas.GenerarQrReserva(r, urlBase));
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

    // [#11] Vista pública de verificación de reserva firmada con HMAC-SHA256
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Verificar(int id, string? f)
    {
        if (string.IsNullOrWhiteSpace(f))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View((ReservaVerificadaDto?)null);
        }

        var verificada = await _reservas.ObtenerReservaVerificadaAsync(id, f);
        if (verificada is null)
            Response.StatusCode = StatusCodes.Status404NotFound;

        return View(verificada);
    }
}
