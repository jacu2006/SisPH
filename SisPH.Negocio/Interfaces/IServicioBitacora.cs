using SisPH.Entidades.Entidades;

namespace SisPH.Negocio.Interfaces;

public record RegistroBitacoraAuditoria(Bitacora Registro, bool FirmaValida);

public interface IServicioBitacora
{
    Task RegistrarAsync(int? usuarioId, string accion, string entidad, string? entidadId = null, string? detalle = null);
    bool VerificarFila(Bitacora registro);
    Task<List<RegistroBitacoraAuditoria>> ConsultarBitacoraAsync(DateTime? desde, DateTime? hasta, int? usuarioId, string? accion);
    Task<List<LogError>> ConsultarErroresAsync(DateTime? desde, DateTime? hasta, string? origen);
}