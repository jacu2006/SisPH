using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public interface IServicioIncidencias
{
    Task<List<Incidencia>> ListarAsync(EstadoIncidencia? estado = null, int? residenteId = null);
    Task<Incidencia?> ObtenerPorIdAsync(int id);
    Task<ResultadoOperacion> ReportarAsync(
        int residenteId,
        string titulo,
        string descripcion,
        ArchivoSubido? foto,
        string rutaFisicaWwwroot,
        int usuarioId);

    Task<ResultadoOperacion> TomarAsync(int incidenciaId, int operadorId);
    Task<ResultadoOperacion> ResolverAsync(int incidenciaId, string observacionTecnica, int operadorId);
    Task<ResultadoOperacion> CerrarAsync(int incidenciaId, int usuarioId);
    Task<ResultadoOperacion> ReabrirAsync(int incidenciaId, string motivoReapertura, int usuarioId);
}