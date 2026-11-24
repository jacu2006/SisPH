using SisPH.Entidades.Enums;

namespace SisPH.Entidades.Entidades;

public class Incidencia
{
    public int Id { get; set; }
    public int ResidenteId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Foto { get; set; }
    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;
    public string? ObservacionTecnica { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public Residente? Residente { get; set; }

    public void CambiarEstado(EstadoIncidencia nuevoEstado) => Estado = nuevoEstado;

    public void Cerrar(string obs)
    {
        ObservacionTecnica = obs;
        Estado = EstadoIncidencia.Cerrada;
    }
}