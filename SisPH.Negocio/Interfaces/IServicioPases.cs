using SisPH.Entidades.Entidades;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public record PaseGeneradoDto(PaseVisita Pase, string QrBase64);

public interface IServicioPases
{
    Task<List<PaseVisita>> ListarPorResidenteAsync(int residenteId);
    Task<List<PaseVisita>> ListarHistorialGaritaAsync(DateTime? fecha = null);
    Task<ResultadoOperacion<PaseGeneradoDto>> GenerarPaseAsync(
        int residenteId,
        string nombreVisitante,
        string? cedulaVisitante,
        DateTime vigenteHasta,
        int usuarioId);

    Task<ResultadoOperacion<PaseVisita>> RegistrarEscaneoGaritaAsync(string tokenQr, int operadorId);
}