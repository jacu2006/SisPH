using SisPH.Entidades.Entidades;

namespace SisPH.Negocio.Interfaces;

public record EstadoCuentaResidenteDto(
    Residente Residente,
    Unidad? UnidadVigente,
    bool EstaPazYSalvo,
    decimal TotalAdeudado,
    List<Cuota> Cuotas,
    List<Pago> Pagos,
    List<Reserva> Reservas,
    List<Incidencia> Incidencias,
    List<PaseVisita> Pases);

public interface IServicioCuenta
{
    Task<EstadoCuentaResidenteDto?> ObtenerPortalResidenteAsync(int usuarioId);
}