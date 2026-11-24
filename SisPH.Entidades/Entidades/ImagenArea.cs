namespace SisPH.Entidades.Entidades;

public class ImagenArea
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public string RutaThumbnail { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }

    public AreaSocial? AreaSocial { get; set; }
}