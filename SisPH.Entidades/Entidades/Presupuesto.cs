namespace SisPH.Entidades.Entidades;

public class Presupuesto
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Ingreso"; // Ingreso o Gasto
    public decimal MontoProyectado { get; set; }
}