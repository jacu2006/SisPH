using SisPH.Entidades.Enums;

namespace SisPH.Entidades.Entidades;

public class Mudanza
{
    public int Id { get; set; }
    public int UnidadId { get; set; }
    public int UsuarioId { get; set; }
    public TipoMudanza Tipo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string? Observacion { get; set; }

    public Unidad? Unidad { get; set; }
    public Usuario? Usuario { get; set; }

    public void Validar(Usuario u)
    {
        UsuarioId = u.Id;
        Fecha = DateTime.Now;
    }
}