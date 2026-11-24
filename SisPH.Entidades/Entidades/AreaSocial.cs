using SisPH.Entidades.Interfaces;

namespace SisPH.Entidades.Entidades;

public class AreaSocial : IDesactivable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Capacidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Deposito { get; set; }
    public TimeSpan HoraApertura { get; set; }
    public TimeSpan HoraCierre { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<ImagenArea> Imagenes { get; set; } = new List<ImagenArea>();
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public void DarDeBaja() => Activo = false;

    public ImagenArea? ImagenPrincipal() =>
        Imagenes.FirstOrDefault(i => i.EsPrincipal) ?? Imagenes.FirstOrDefault();

    public bool EstaDisponible(TimeSpan inicio, TimeSpan fin) =>
        Activo && inicio >= HoraApertura && fin <= HoraCierre && fin > inicio;
}