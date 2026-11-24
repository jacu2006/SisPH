using SisPH.Entidades.Enums;

namespace SisPH.Entidades.Entidades;

public class Ocupacion
{
    public int Id { get; set; }
    public int UnidadId { get; set; }
    public int ResidenteId { get; set; }
    public TipoOcupacion Tipo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public Unidad? Unidad { get; set; }
    public Residente? Residente { get; set; }

    public void Cerrar(DateTime fecha) => FechaFin = fecha.Date;

    public bool EstaVigente() => !FechaFin.HasValue;
}