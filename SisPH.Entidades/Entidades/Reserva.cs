using SisPH.Entidades.Enums;

namespace SisPH.Entidades.Entidades;

public class Reserva
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public int ResidenteId { get; set; }
    public int? AprobadaPor { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Solicitada;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;

    public AreaSocial? AreaSocial { get; set; }
    public Residente? Residente { get; set; }
    public Usuario? UsuarioAprobador { get; set; }

    public void Aprobar(Usuario u)
    {
        Estado = EstadoReserva.Aprobada;
        AprobadaPor = u.Id;
        MotivoRechazo = null;
    }

    public void Rechazar(string motivo)
    {
        Estado = EstadoReserva.Rechazada;
        MotivoRechazo = motivo;
    }

    public void Cancelar() => Estado = EstadoReserva.Cancelada;

    public bool SeCruzaCon(Reserva otra) =>
        AreaId == otra.AreaId &&
        Fecha.Date == otra.Fecha.Date &&
        HoraInicio < otra.HoraFin &&
        HoraFin > otra.HoraInicio;
}