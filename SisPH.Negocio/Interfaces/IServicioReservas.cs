using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public record ReservaVerificadaDto(Reserva Reserva, bool FirmaValida);

public interface IServicioReservas
{
    Task<List<Reserva>> ListarAsync(EstadoReserva? estado = null, int? residenteId = null);
    Task<Reserva?> ObtenerPorIdAsync(int id);
    Task<List<(TimeSpan Inicio, TimeSpan Fin)>> ObtenerFranjasOcupadasAsync(int areaId, DateTime fecha);
    Task<ResultadoOperacion> SolicitarAsync(int areaId, int residenteId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin, int usuarioId);
    Task<ResultadoOperacion<string>> AprobarAsync(int reservaId, int adminId, string urlBaseVerificacion);
    Task<ResultadoOperacion> RechazarAsync(int reservaId, string motivo, int adminId);
    Task<ResultadoOperacion> CancelarAsync(int reservaId, int usuarioId);
    string GenerarQrReserva(Reserva reserva, string urlBaseVerificacion);
    Task<ReservaVerificadaDto?> ObtenerReservaVerificadaAsync(int reservaId, string? firmaRecibida);
}