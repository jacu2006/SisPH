using SisPH.Entidades.Entidades;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public interface IServicioUsuarios
{
    Task<List<Usuario>> ListarAsync(bool? soloActivos = null);
    Task<List<Rol>> ListarRolesAsync();
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<ResultadoOperacion<string>> CrearAsync(string nombre, string correo, int rolId, int adminId);
    Task<ResultadoOperacion> ModificarAsync(int id, string nombre, string correo, int rolId, int adminId);
    Task<ResultadoOperacion> CambiarEstadoActivoAsync(int id, bool activo, int adminId);
}