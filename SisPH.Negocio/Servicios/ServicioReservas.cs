using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;
using SisPH.Seguridad;

namespace SisPH.Negocio.Servicios;

public class ServicioReservas : IServicioReservas
{
    private readonly IRepositorio<Reserva> _repoReserva;
    private readonly IRepositorio<AreaSocial> _repoArea;
    private readonly IRepositorio<Ocupacion> _repoOcupacion;
    private readonly IServicioCuotas _servicioCuotas;
    private readonly ITransformacionSegura _firma;
    private readonly IServicioQR _qr;
    private readonly IServicioBitacora _bitacora;

    public ServicioReservas(
        IRepositorio<Reserva> repoReserva,
        IRepositorio<AreaSocial> repoArea,
        IRepositorio<Ocupacion> repoOcupacion,
        IServicioCuotas servicioCuotas,
        [FromKeyedServices("firma")] ITransformacionSegura firma,
        IServicioQR qr,
        IServicioBitacora bitacora)
    {
        _repoReserva = repoReserva;
        _repoArea = repoArea;
        _repoOcupacion = repoOcupacion;
        _servicioCuotas = servicioCuotas;
        _firma = firma;
        _qr = qr;
        _bitacora = bitacora;
    }

    public async Task<List<Reserva>> ListarAsync(EstadoReserva? estado = null, int? residenteId = null)
    {
        var query = _repoReserva.Consultar()
            .Include(r => r.AreaSocial)
            .Include(r => r.Residente)
            .Include(r => r.UsuarioAprobador)
            .AsNoTracking()
            .AsSplitQuery();

        if (estado.HasValue)
            query = query.Where(r => r.Estado == estado.Value);

        if (residenteId.HasValue)
            query = query.Where(r => r.ResidenteId == residenteId.Value);

        return await query
            .OrderByDescending(r => r.Fecha)
            .ThenBy(r => r.HoraInicio)
            .ToListAsync();
    }

    public async Task<Reserva?> ObtenerPorIdAsync(int id) =>
        await _repoReserva.Consultar()
            .Include(r => r.AreaSocial)
            .Include(r => r.Residente)
            .Include(r => r.UsuarioAprobador)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<(TimeSpan Inicio, TimeSpan Fin)>> ObtenerFranjasOcupadasAsync(int areaId, DateTime fecha)
    {
        var dia = fecha.Date;
        var franjas = await _repoReserva.Consultar()
            .AsNoTracking()
            .Where(r =>
                r.AreaId == areaId &&
                r.Fecha.Date == dia &&
                r.Estado == EstadoReserva.Aprobada)
            .OrderBy(r => r.HoraInicio)
            .Select(r => new { r.HoraInicio, r.HoraFin })
            .ToListAsync();

        return franjas.Select(r => (r.HoraInicio, r.HoraFin)).ToList();
    }

    public async Task<ResultadoOperacion> SolicitarAsync(
        int areaId, int residenteId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin, int usuarioId)
    {
        if (fecha.Date < DateTime.Today)
            return ResultadoOperacion.Error("No puedes solicitar reservas para fechas pasadas.");

        // [#20] Rechazar solicitudes para hoy cuya hora de inicio ya pasó
        if (fecha.Date == DateTime.Today && horaInicio <= DateTime.Now.TimeOfDay)
            return ResultadoOperacion.Error("No puedes solicitar una reserva para hoy en un horario que ya inició o pasó.");

        if (horaFin <= horaInicio)
            return ResultadoOperacion.Error("La hora de fin debe ser posterior a la hora de inicio.");

        var area = await _repoArea.ObtenerPorIdAsync(areaId);
        if (area is null || !area.Activo)
            return ResultadoOperacion.Error("El área social seleccionada no está disponible.");

        if (!area.EstaDisponible(horaInicio, horaFin))
            return ResultadoOperacion.Error(
                $"El horario solicitado está fuera del horario permitido del área ({area.HoraApertura:hh\\:mm} a {area.HoraCierre:hh\\:mm}).");

        // [#6 y #19] Verificar paz y salvo dinámico de la unidad vigente del residente
        var ocupacion = await _repoOcupacion.Consultar()
            .FirstOrDefaultAsync(o => o.ResidenteId == residenteId && o.FechaFin == null);

        if (ocupacion is null)
            return ResultadoOperacion.Error("No tienes una unidad activa asociada para realizar reservas.");

        if (!await _servicioCuotas.UnidadEstaPazYSalvoAsync(ocupacion.UnidadId))
            return ResultadoOperacion.Error("No puedes solicitar reservas porque tu unidad tiene cuotas vencidas o morosas.");

        var candidata = new Reserva
        {
            AreaId = areaId,
            ResidenteId = residenteId,
            Fecha = fecha.Date,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Estado = EstadoReserva.Solicitada,
            FechaSolicitud = DateTime.Now
        };

        var aprobadasMismoDia = await _repoReserva.BuscarAsync(r =>
            r.AreaId == areaId &&
            r.Fecha.Date == fecha.Date &&
            r.Estado == EstadoReserva.Aprobada);

        if (aprobadasMismoDia.Any(a => a.SeCruzaCon(candidata)))
            return ResultadoOperacion.Error("El horario seleccionado choca con otra reserva ya aprobada para esa fecha.");

        await _repoReserva.AgregarAsync(candidata);

        await _bitacora.RegistrarAsync(
            usuarioId,
            "SolicitarReserva",
            "Reserva",
            candidata.Id.ToString(),
            $"Solicitud de reserva para {area.Nombre} el {fecha:dd/MM/yyyy} ({horaInicio:hh\\:mm}-{horaFin:hh\\:mm}).");

        return ResultadoOperacion.Ok("Solicitud de reserva enviada. Queda pendiente de aprobación.");
    }

    public async Task<ResultadoOperacion<string>> AprobarAsync(int reservaId, int adminId, string urlBaseVerificacion)
    {
        var reserva = await ObtenerPorIdAsync(reservaId);
        if (reserva is null)
            return ResultadoOperacion<string>.Error("La reserva no existe.");

        if (reserva.Estado != EstadoReserva.Solicitada)
            return ResultadoOperacion<string>.Error("Solo se pueden aprobar reservas en estado Solicitada.");

        // [#19] Revalidar Paz y Salvo dinámico al momento de aprobar la reserva
        var ocupacion = await _repoOcupacion.Consultar()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.ResidenteId == reserva.ResidenteId && o.FechaFin == null);

        if (ocupacion is null)
            return ResultadoOperacion<string>.Error("No se puede aprobar: el residente ya no cuenta con una unidad activa.");

        if (!await _servicioCuotas.UnidadEstaPazYSalvoAsync(ocupacion.UnidadId))
            return ResultadoOperacion<string>.Error("No se puede aprobar la reserva: la unidad del residente tiene cuotas vencidas o morosas.");

        var aprobadasMismoDia = await _repoReserva.BuscarAsync(r =>
            r.Id != reservaId &&
            r.AreaId == reserva.AreaId &&
            r.Fecha.Date == reserva.Fecha.Date &&
            r.Estado == EstadoReserva.Aprobada);

        if (aprobadasMismoDia.Any(a => a.SeCruzaCon(reserva)))
            return ResultadoOperacion<string>.Error("No se puede aprobar: se detectó un choque de horario con otra reserva aprobada.");

        reserva.Estado = EstadoReserva.Aprobada;
        reserva.AprobadaPor = adminId;
        reserva.MotivoRechazo = null;

        await _repoReserva.ModificarAsync(reserva);

        // [#11] QR firmado apuntando a Reservas/Verificar/{id}?f={firmaHmac}
        var qrBase64 = GenerarQrReserva(reserva, urlBaseVerificacion);

        await _bitacora.RegistrarAsync(
            adminId,
            "AprobarReserva",
            "Reserva",
            reserva.Id.ToString(),
            $"Reserva #{reserva.Id} aprobada.");

        return ResultadoOperacion<string>.Ok(qrBase64, "Reserva aprobada y código QR generado.");
    }

    public async Task<ResultadoOperacion> RechazarAsync(int reservaId, string motivo, int adminId)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            return ResultadoOperacion.Error("Debe indicar el motivo del rechazo.");

        var reserva = await _repoReserva.ObtenerPorIdAsync(reservaId);
        if (reserva is null)
            return ResultadoOperacion.Error("La reserva no existe.");

        if (reserva.Estado != EstadoReserva.Solicitada)
            return ResultadoOperacion.Error("Solo se pueden rechazar reservas en estado Solicitada.");

        reserva.Rechazar(motivo.Trim());
        await _repoReserva.ModificarAsync(reserva);

        await _bitacora.RegistrarAsync(
            adminId,
            "RechazarReserva",
            "Reserva",
            reserva.Id.ToString(),
            $"Reserva #{reserva.Id} rechazada. Motivo: {reserva.MotivoRechazo}");

        return ResultadoOperacion.Ok("Reserva rechazada.");
    }

    public async Task<ResultadoOperacion> CancelarAsync(int reservaId, int usuarioId)
    {
        var reserva = await _repoReserva.ObtenerPorIdAsync(reservaId);
        if (reserva is null)
            return ResultadoOperacion.Error("La reserva no existe.");

        if (reserva.Estado != EstadoReserva.Solicitada && reserva.Estado != EstadoReserva.Aprobada)
            return ResultadoOperacion.Error("Esta reserva no puede cancelarse en su estado actual.");

        if (reserva.Fecha.Date < DateTime.Today)
            return ResultadoOperacion.Error("No se puede cancelar una reserva cuya fecha ya pasó.");

        reserva.Cancelar();
        await _repoReserva.ModificarAsync(reserva);

        await _bitacora.RegistrarAsync(
            usuarioId,
            "CancelarReserva",
            "Reserva",
            reserva.Id.ToString(),
            $"Reserva #{reserva.Id} cancelada.");

        return ResultadoOperacion.Ok("Reserva cancelada correctamente.");
    }

    public string GenerarQrReserva(Reserva reserva, string urlBaseVerificacion)
    {
        var firma = _firma.Transformar(ConstruirPayloadReserva(reserva));
        var url = $"{urlBaseVerificacion.TrimEnd('/')}/{reserva.Id}?f={Uri.EscapeDataString(firma)}";
        return _qr.GenerarPngBase64(url);
    }

    public async Task<ReservaVerificadaDto?> ObtenerReservaVerificadaAsync(int reservaId, string? firmaRecibida)
    {
        if (string.IsNullOrWhiteSpace(firmaRecibida))
            return null;

        var reserva = await _repoReserva.Consultar()
            .Include(r => r.AreaSocial)
            .Include(r => r.Residente)
            .Include(r => r.UsuarioAprobador)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservaId);

        if (reserva is null)
            return null;

        bool firmaValida = _firma.Verificar(ConstruirPayloadReserva(reserva), firmaRecibida.Trim());
        if (!firmaValida)
            return null;

        return new ReservaVerificadaDto(reserva, true);
    }

    private static string ConstruirPayloadReserva(Reserva r) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"RESERVA|{r.Id}|{r.AreaId}|{r.ResidenteId}|{r.Fecha:yyyy-MM-dd}|{r.HoraInicio:hh\\:mm}|{r.HoraFin:hh\\:mm}");
}
