using SisPH.Entidades.Interfaces;

namespace SisPH.Entidades.Entidades;

public class Residente : IDesactivable
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Foto { get; set; }
    public string? Telefono { get; set; }
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }
    public ICollection<Ocupacion> Ocupaciones { get; set; } = new List<Ocupacion>();
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();
    public ICollection<PaseVisita> PasesVisita { get; set; } = new List<PaseVisita>();

    public string NombreCompleto() => $"{Nombre} {Apellido}".Trim();

    public void DarDeBaja() => Activo = false;
}