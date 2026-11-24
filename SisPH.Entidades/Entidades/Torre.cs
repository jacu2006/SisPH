namespace SisPH.Entidades.Entidades;

public class Torre
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Pisos { get; set; }

    public ICollection<Unidad> Unidades { get; set; } = new List<Unidad>();

    public List<Unidad> ObtenerUnidades() => Unidades.Where(u => u.Activo).ToList();
}