using System.ComponentModel.DataAnnotations.Schema;
using SisPH.Entidades.Interfaces;

namespace SisPH.Entidades.Entidades;

public class Usuario : IDesactivable
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; } = 0;
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [NotMapped]
    public bool DebeCambiarPassword { get; set; }

    public Rol? Rol { get; set; }
    public Residente? Residente { get; set; }

    public void DarDeBaja() => Activo = false;

    public void Bloquear(int minutos) => BloqueadoHasta = DateTime.Now.AddMinutes(minutos);

    public bool EstaBloqueado() => BloqueadoHasta.HasValue && BloqueadoHasta.Value > DateTime.Now;
}