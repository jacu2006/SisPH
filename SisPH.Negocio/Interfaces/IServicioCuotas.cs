using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public record VistaPreviaCuotaItem(
    int UnidadId,
    string Torre,
    string NumeroUnidad,
    decimal Coeficiente,
    decimal MontoCalculado);

public interface IServicioCuotas
{
    Task<List<Cuota>> ListarPorPeriodoAsync(string? periodo = null, int? torreId = null, EstadoCuota? estado = null);
    Task<List<Cuota>> ListarPendientesPorUnidadAsync(int unidadId);
    Task<bool> UnidadEstaPazYSalvoAsync(int unidadId);
    Task<ResultadoOperacion<List<VistaPreviaCuotaItem>>> GenerarVistaPreviaAsync(string periodo, decimal presupuesto, TipoCuota tipo);
    Task<ResultadoOperacion> GenerarCuotasAsync(string periodo, decimal presupuesto, TipoCuota tipo, DateTime? vence, int adminId);
    decimal ObtenerPorcentajeRecargoMora();
    Task<ResultadoOperacion> ActualizarEstadosYRecargosAsync(decimal? porcentajeRecargo = null, int? usuarioId = null);
}