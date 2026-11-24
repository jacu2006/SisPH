namespace SisPH.Entidades.Entidades;

public class PaseVisita
{
    public int Id { get; set; }
    public int ResidenteId { get; set; }
    public string NombreVisitante { get; set; } = string.Empty;
    public string? CedulaVisitante { get; set; }
    public string TokenQR { get; set; } = string.Empty;
    public DateTime VigenteHasta { get; set; }
    public DateTime? Entrada { get; set; }
    public DateTime? Salida { get; set; }

    public Residente? Residente { get; set; }

    public bool EsValido() => DateTime.Now <= VigenteHasta && !Salida.HasValue;

    public void MarcarEntrada() => Entrada = DateTime.Now;

    public void MarcarSalida() => Salida = DateTime.Now;
}