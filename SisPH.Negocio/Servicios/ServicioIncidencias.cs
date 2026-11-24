using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioIncidencias : IServicioIncidencias
{
    private readonly IRepositorio<Incidencia> _repoIncidencia;
    private readonly IServicioArchivos _archivos;
    private readonly IServicioBitacora _bitacora;

    public ServicioIncidencias(
        IRepositorio<Incidencia> repoIncidencia,
        IServicioArchivos archivos,
        IServicioBitacora bitacora)
    {
        _repoIncidencia = repoIncidencia;
        _archivos = archivos;
        _bitacora = bitacora;
    }

    public async Task<List<Incidencia>> ListarAsync(EstadoIncidencia? estado = null, int? residenteId = null)
    {
        var query = _repoIncidencia.Consultar()
            .Include(i => i.Residente)
            .AsNoTracking();

        if (estado.HasValue)
            query = query.Where(i => i.Estado == estado.Value);

        if (residenteId.HasValue)
            query = query.Where(i => i.ResidenteId == residenteId.Value);

        return await query.OrderByDescending(i => i.Fecha).ToListAsync();
    }

    public async Task<Incidencia?> ObtenerPorIdAsync(int id) =>
        await _repoIncidencia.Consultar()
            .Include(i => i.Residente)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<ResultadoOperacion> ReportarAsync(
        int residenteId,
        string titulo,
        string descripcion,
        ArchivoSubido? foto,
        string rutaFisicaWwwroot,
        int usuarioId)
    {
        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion))
            return ResultadoOperacion.Error("Debe completar el título y la descripción de la incidencia.");

        string? rutaFoto = null;
        if (foto is not null && foto.TamanioBytes > 0)
        {
            try
            {
                var guardado = await _archivos.GuardarImagenAsync(
                    foto.Contenido,
                    foto.NombreOriginal,
                    foto.TamanioBytes,
                    "incidencias",
                    rutaFisicaWwwroot,
                    generarThumbnail: false);
                rutaFoto = guardado.RutaRelativa;
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        var incidencia = new Incidencia
        {
            ResidenteId = residenteId,
            Titulo = titulo.Trim(),
            Descripcion = descripcion.Trim(),
            Foto = rutaFoto,
            Estado = EstadoIncidencia.Abierta,
            Fecha = DateTime.Now
        };

        await _repoIncidencia.AgregarAsync(incidencia);

        await _bitacora.RegistrarAsync(
            usuarioId,
            "ReportarIncidencia",
            "Incidencia",
            incidencia.Id.ToString(),
            $"Incidencia reportada: {incidencia.Titulo}.");

        return ResultadoOperacion.Ok("Incidencia reportada correctamente.");
    }

    public async Task<ResultadoOperacion> TomarAsync(int incidenciaId, int operadorId)
    {
        var incidencia = await _repoIncidencia.ObtenerPorIdAsync(incidenciaId);
        if (incidencia is null)
            return ResultadoOperacion.Error("La incidencia no existe.");

        if (incidencia.Estado != EstadoIncidencia.Abierta)
            return ResultadoOperacion.Error("Solo se pueden tomar incidencias en estado Abierta.");

        incidencia.CambiarEstado(EstadoIncidencia.EnProceso);
        await _repoIncidencia.ModificarAsync(incidencia);

        await _bitacora.RegistrarAsync(
            operadorId,
            "TomarIncidencia",
            "Incidencia",
            incidencia.Id.ToString(),
            "Incidencia pasada a EnProceso.");

        return ResultadoOperacion.Ok("Incidencia tomada en proceso.");
    }

    public async Task<ResultadoOperacion> ResolverAsync(int incidenciaId, string observacionTecnica, int operadorId)
    {
        if (string.IsNullOrWhiteSpace(observacionTecnica))
            return ResultadoOperacion.Error("La observación técnica es obligatoria para marcar la incidencia como Resuelta.");

        var incidencia = await _repoIncidencia.ObtenerPorIdAsync(incidenciaId);
        if (incidencia is null)
            return ResultadoOperacion.Error("La incidencia no existe.");

        if (incidencia.Estado != EstadoIncidencia.EnProceso && incidencia.Estado != EstadoIncidencia.Abierta)
            return ResultadoOperacion.Error("La incidencia no puede resolverse desde su estado actual.");

        incidencia.ObservacionTecnica = observacionTecnica.Trim();
        incidencia.CambiarEstado(EstadoIncidencia.Resuelta);
        await _repoIncidencia.ModificarAsync(incidencia);

        await _bitacora.RegistrarAsync(
            operadorId,
            "ResolverIncidencia",
            "Incidencia",
            incidencia.Id.ToString(),
            $"Incidencia resuelta. Obs: {incidencia.ObservacionTecnica}");

        return ResultadoOperacion.Ok("Incidencia marcada como Resuelta.");
    }

    public async Task<ResultadoOperacion> CerrarAsync(int incidenciaId, int usuarioId)
    {
        var incidencia = await _repoIncidencia.ObtenerPorIdAsync(incidenciaId);
        if (incidencia is null)
            return ResultadoOperacion.Error("La incidencia no existe.");

        if (incidencia.Estado != EstadoIncidencia.Resuelta)
            return ResultadoOperacion.Error("Solo pueden cerrarse incidencias que ya fueron marcadas como Resueltas.");

        incidencia.CambiarEstado(EstadoIncidencia.Cerrada);
        await _repoIncidencia.ModificarAsync(incidencia);

        await _bitacora.RegistrarAsync(
            usuarioId,
            "CerrarIncidencia",
            "Incidencia",
            incidencia.Id.ToString(),
            "Incidencia cerrada.");

        return ResultadoOperacion.Ok("Incidencia cerrada correctamente.");
    }

    public async Task<ResultadoOperacion> ReabrirAsync(int incidenciaId, string motivoReapertura, int usuarioId)
    {
        var incidencia = await _repoIncidencia.ObtenerPorIdAsync(incidenciaId);
        if (incidencia is null)
            return ResultadoOperacion.Error("La incidencia no existe.");

        if (incidencia.Estado != EstadoIncidencia.Resuelta)
            return ResultadoOperacion.Error("Solo se pueden reabrir incidencias en estado Resuelta.");

        var nota = string.IsNullOrWhiteSpace(motivoReapertura) ? "Residente no conforme." : motivoReapertura.Trim();
        incidencia.ObservacionTecnica = $"{incidencia.ObservacionTecnica} | [Reabierta]: {nota}".Trim();
        incidencia.CambiarEstado(EstadoIncidencia.EnProceso);
        await _repoIncidencia.ModificarAsync(incidencia);

        await _bitacora.RegistrarAsync(
            usuarioId,
            "ReabrirIncidencia",
            "Incidencia",
            incidencia.Id.ToString(),
            $"Incidencia reabierta: {nota}");

        return ResultadoOperacion.Ok("La incidencia volvió a estado En proceso.");
    }
}