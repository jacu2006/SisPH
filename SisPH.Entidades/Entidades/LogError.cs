namespace SisPH.Entidades.Entidades;

public class LogError
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Origen { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Ruta { get; set; }
    public string? Ip { get; set; }

    public Usuario? Usuario { get; set; }
}