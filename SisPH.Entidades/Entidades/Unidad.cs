using SisPH.Entidades.Enums;
using SisPH.Entidades.Interfaces;

namespace SisPH.Entidades.Entidades;

public class Unidad : IDesactivable
{
    public int Id { get; set; }
    public int TorreId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int Piso { get; set; }
    public decimal Metros2 { get; set; }
    public decimal Coeficiente { get; set; }
    public bool Activo { get; set; } = true;

    public Torre? Torre { get; set; }
    public ICollection<Ocupacion> Ocupaciones { get; set; } = new List<Ocupacion>();
    public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    public ICollection<Mudanza> Mudanzas { get; set; } = new List<Mudanza>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public void DarDeBaja() => Activo = false;

    public decimal CalcularCuota(decimal presupuestoMensual) =>
        Math.Round(presupuestoMensual * (Coeficiente / 100m), 2);

    public bool EstaPazYSalvo() =>
        !Cuotas.Any(c => c.Estado == EstadoCuota.Vencida || c.Estado == EstadoCuota.Morosa);
}