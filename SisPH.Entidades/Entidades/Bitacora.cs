namespace SisPH.Entidades.Entidades;

public class Bitacora
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public string Firma { get; set; } = string.Empty;

    public Usuario? Usuario { get; set; }
}