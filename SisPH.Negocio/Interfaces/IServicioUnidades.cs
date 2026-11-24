using SisPH.Entidades.Entidades;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public interface IServicioUnidades
{
    Task<List<Torre>> ListarTorresAsync();
    Task<ResultadoOperacion> CrearTorreAsync(string nombre, int pisos, int adminId);
    Task<List<Unidad>> ListarUnidadesAsync(int? torreId = null, bool? soloActivas = null);
    Task<Unidad?> ObtenerUnidadPorIdAsync(int id);
    Task<decimal> ObtenerSumaCoeficientesTorreAsync(int torreId, int? excluirUnidadId = null);
    Task<ResultadoOperacion> CrearUnidadAsync(int torreId, string numero, int piso, decimal metros2, decimal coeficiente, int adminId);
    Task<ResultadoOperacion> ModificarUnidadAsync(int id, int torreId, string numero, int piso, decimal metros2, decimal coeficiente, int adminId);
    Task<ResultadoOperacion> CambiarEstadoUnidadAsync(int id, bool activo, int adminId);
}