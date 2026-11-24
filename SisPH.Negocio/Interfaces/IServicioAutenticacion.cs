using SisPH.Entidades.Entidades;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public interface IServicioAutenticacion
{
    Task<ResultadoOperacion<Usuario>> LoginAsync(string correo, string clave);
    Task<ResultadoOperacion> CambiarPasswordAsync(int usuarioId, string claveActual, string claveNueva);
}