using SisPH.Entidades.Enums;

namespace SisPH.Entidades.Entidades;

public class Pago
{
    public int Id { get; set; }
    public int UnidadId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public decimal Monto { get; set; }
    public MetodoPago Metodo { get; set; }
    public string? Referencia { get; set; }
    public string Firma { get; set; } = string.Empty;

    public Unidad? Unidad { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<PagoDetalle> Detalles { get; set; } = new List<PagoDetalle>();
}