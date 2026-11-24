using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public record ArchivoSubido(Stream Contenido, string NombreOriginal, long TamanioBytes);

public interface IServicioResidentes
{
    Task<List<Residente>> ListarAsync(bool? soloActivos = null);
    Task<Residente?> ObtenerPorIdAsync(int id);
    Task<ResultadoOperacion<string?>> CrearAsync(
        string cedula,
        string nombre,
        string apellido,
        string? telefono,
        string correo,
        int unidadId,
        TipoOcupacion tipoOcupacion,
        bool crearUsuarioPortal,
        ArchivoSubido? foto,
        string rutaFisicaWwwroot,
        int adminId);

    Task<ResultadoOperacion> ModificarAsync(
        int id,
        string cedula,
        string nombre,
        string apellido,
        string? telefono,
        string correo,
        ArchivoSubido? nuevaFoto,
        string rutaFisicaWwwroot,
        int adminId);

    Task<ResultadoOperacion> DarDeBajaAsync(int id, int adminId);
}