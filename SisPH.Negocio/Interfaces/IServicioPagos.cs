using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public record ReciboVerificadoDto(
    Pago Pago,
    bool FirmaValida,
    string QrBase64);

public interface IServicioPagos
{
    Task<List<Pago>> ListarRecientesAsync(int? unidadId = null);
    Task<ResultadoOperacion<ReciboVerificadoDto>> RegistrarPagoAsync(
        int unidadId,
        List<int> cuotaIds,
        decimal montoPagado,
        MetodoPago metodo,
        string? referencia,
        string urlBaseVerificacion,
        int operadorId);

    Task<ReciboVerificadoDto?> ObtenerReciboVerificadoAsync(int pagoId, string urlBaseVerificacion);
    Task<ReciboVerificadoDto?> VerificarReciboPublicoAsync(int pagoId, string? firmaRecibida, string urlBaseVerificacion);
}