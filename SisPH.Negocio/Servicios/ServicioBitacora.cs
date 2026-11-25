using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Interfaces;
using SisPH.Seguridad;

namespace SisPH.Negocio.Servicios;

public class ServicioBitacora : IServicioBitacora
{
    private readonly IRepositorio<Bitacora> _repoBitacora;
    private readonly IRepositorio<LogError> _repoLogError;
    private readonly ITransformacionSegura _firma;

    public ServicioBitacora(
        IRepositorio<Bitacora> repoBitacora,
        IRepositorio<LogError> repoLogError,
        [FromKeyedServices("firma")] ITransformacionSegura firma)
    {
        _repoBitacora = repoBitacora;
        _repoLogError = repoLogError;
        _firma = firma;
    }

    public async Task RegistrarAsync(int? usuarioId, string accion, string entidad, string? entidadId = null, string? detalle = null)
    {
        var fechaPrecisa = DateTime.Now;

        var registro = new Bitacora
        {
            UsuarioId = usuarioId,
            Fecha = fechaPrecisa,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle
        };

        registro.Firma = _firma.Transformar(ConstruirPayload(registro));
        await _repoBitacora.AgregarAsync(registro);
    }

    public bool VerificarFila(Bitacora registro)
    {
        var payload = ConstruirPayload(registro);
        if (_firma.Verificar(payload, registro.Firma))
            return true;

        // Compatibilidad con registros firmados con el formato anterior de milisegundos
        if (registro.Fecha.Ticks % TimeSpan.TicksPerMillisecond == 0)
        {
            var payloadAnterior = ConstruirPayloadAnterior(registro);
            return _firma.Verificar(payloadAnterior, registro.Firma);
        }

        return false;
    }

    public async Task<List<RegistroBitacoraAuditoria>> ConsultarBitacoraAsync(
        DateTime? desde, DateTime? hasta, int? usuarioId, string? accion)
    {
        var query = _repoBitacora.Consultar()
            .Include(b => b.Usuario)
            .AsNoTracking();

        if (desde.HasValue)
            query = query.Where(b => b.Fecha >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(b => b.Fecha < hasta.Value.Date.AddDays(1));

        if (usuarioId.HasValue)
            query = query.Where(b => b.UsuarioId == usuarioId.Value);

        if (!string.IsNullOrWhiteSpace(accion))
            query = query.Where(b => b.Accion.Contains(accion.Trim()));

        var registros = await query
            .OrderByDescending(b => b.Fecha)
            .Take(500)
            .ToListAsync();

        return registros
            .Select(r => new RegistroBitacoraAuditoria(r, VerificarFila(r)))
            .ToList();
    }

    public async Task<List<LogError>> ConsultarErroresAsync(DateTime? desde, DateTime? hasta, string? origen)
    {
        var query = _repoLogError.Consultar()
            .Include(e => e.Usuario)
            .AsNoTracking();

        if (desde.HasValue)
            query = query.Where(e => e.Fecha >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(e => e.Fecha < hasta.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(origen))
            query = query.Where(e => e.Origen.Contains(origen.Trim()));

        return await query
            .OrderByDescending(e => e.Fecha)
            .Take(500)
            .ToListAsync();
    }

    private static string ConstruirPayload(Bitacora b) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{b.UsuarioId?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}|{b.Fecha:yyyy-MM-dd HH:mm:ss.fffffff}|{b.Accion}|{b.Entidad}|{b.EntidadId ?? ""}|{b.Detalle ?? ""}");

    private static string ConstruirPayloadAnterior(Bitacora b) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{b.UsuarioId?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}|{b.Fecha:yyyy-MM-ddTHH:mm:ss.fff}|{b.Accion}|{b.Entidad}|{b.EntidadId ?? ""}|{b.Detalle ?? ""}");
}
