using SisPH.Entidades.Enums;

namespace SisPH.Entidades.Entidades;

public class Cuota
{
    public int Id { get; set; }
    public int UnidadId { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public TipoCuota Tipo { get; set; } = TipoCuota.Ordinaria;
    public decimal Monto { get; set; }
    public decimal Recargo { get; set; } = 0;
    public DateTime Vence { get; set; }
    public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;

    public Unidad? Unidad { get; set; }
    public ICollection<PagoDetalle> PagoDetalles { get; set; } = new List<PagoDetalle>();

    public void AplicarRecargo(decimal pct)
    {
        if (Recargo == 0)
            Recargo = Math.Round(Monto * (pct / 100m), 2);
    }

    public bool EstaVencida() => DateTime.Today > Vence.Date && Estado != EstadoCuota.Pagada;
}