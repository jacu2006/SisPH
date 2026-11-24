namespace SisPH.Entidades.DTOs;

public class MorosidadView
{
    public int UnidadId { get; set; }
    public int TorreId { get; set; }
    public string Torre { get; set; } = string.Empty;
    public string NumeroUnidad { get; set; } = string.Empty;
    public int Piso { get; set; }
    public string Residente { get; set; } = string.Empty;
    public string? Cedula { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public int CuotasVencidas { get; set; }
    public int CuotasMorosas { get; set; }
    public int TotalCuotasAtrasadas { get; set; }
    public decimal SaldoAdeudado { get; set; }
    public DateTime VencimientoMasAntiguo { get; set; }
}