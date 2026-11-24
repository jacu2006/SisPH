namespace SisPH.Entidades.Entidades;

public class PagoDetalle
{
    public int Id { get; set; }
    public int PagoId { get; set; }
    public int CuotaId { get; set; }
    public decimal MontoAplicado { get; set; }

    public Pago? Pago { get; set; }
    public Cuota? Cuota { get; set; }
}