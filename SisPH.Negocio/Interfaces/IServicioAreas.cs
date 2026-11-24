using SisPH.Entidades.Entidades;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public interface IServicioAreas
{
    Task<List<AreaSocial>> ListarAsync(bool? soloActivas = null);
    Task<AreaSocial?> ObtenerPorIdAsync(int id);
    Task<ResultadoOperacion> CrearAsync(
        string nombre,
        string? descripcion,
        int capacidad,
        decimal costo,
        decimal deposito,
        TimeSpan horaApertura,
        TimeSpan horaCierre,
        List<ArchivoSubido> imagenes,
        int indicePrincipal,
        string rutaFisicaWwwroot,
        int adminId);

    Task<ResultadoOperacion> ModificarAsync(
        int id,
        string nombre,
        string? descripcion,
        int capacidad,
        decimal costo,
        decimal deposito,
        TimeSpan horaApertura,
        TimeSpan horaCierre,
        List<ArchivoSubido>? nuevasImagenes,
        int? imagenPrincipalId,
        string rutaFisicaWwwroot,
        int adminId);

    Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo, int adminId);
}