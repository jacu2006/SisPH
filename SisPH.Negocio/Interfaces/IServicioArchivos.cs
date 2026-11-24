namespace SisPH.Negocio.Interfaces;

public record ResultadoArchivo(string RutaRelativa, string? RutaThumbnailRelativa);

public interface IServicioArchivos
{
    Task<ResultadoArchivo> GuardarImagenAsync(
        Stream contenido,
        string nombreOriginal,
        long tamanioBytes,
        string subcarpeta,
        string rutaFisicaWwwroot,
        bool generarThumbnail = false,
        int anchoThumbnailPx = 200);
}