using SisPH.Negocio.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SisPH.Negocio.Servicios;

public class ServicioArchivos : IServicioArchivos
{
    private const long MaxBytes = 2L * 1024 * 1024; // 2 MB según sección 4.3
    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public async Task<ResultadoArchivo> GuardarImagenAsync(
        Stream contenido,
        string nombreOriginal,
        long tamanioBytes,
        string subcarpeta,
        string rutaFisicaWwwroot,
        bool generarThumbnail = false,
        int anchoThumbnailPx = 200)
    {
        if (tamanioBytes <= 0)
            throw new InvalidOperationException("El archivo seleccionado está vacío.");

        if (tamanioBytes > MaxBytes)
            throw new InvalidOperationException("La imagen supera el tamaño máximo permitido de 2 MB.");

        var extension = Path.GetExtension(nombreOriginal);
        if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
            throw new InvalidOperationException("Formato de imagen no permitido. Solo se aceptan archivos JPG, PNG o WEBP.");

        var carpetaDestino = Path.Combine(rutaFisicaWwwroot, "uploads", subcarpeta);
        Directory.CreateDirectory(carpetaDestino);

        var nombreBase = Guid.NewGuid().ToString("N");
        var nombreArchivo = $"{nombreBase}{extension.ToLowerInvariant()}";
        var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

        using var imagen = await Image.LoadAsync(contenido);
        await imagen.SaveAsync(rutaFisica);

        var rutaRelativa = $"/uploads/{subcarpeta}/{nombreArchivo}";
        string? rutaThumbnailRelativa = null;

        if (generarThumbnail)
        {
            var nombreThumb = $"{nombreBase}_thumb.jpg";
            var rutaFisicaThumb = Path.Combine(carpetaDestino, nombreThumb);

            imagen.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(anchoThumbnailPx, 0),
                Mode = ResizeMode.Max
            }));

            await imagen.SaveAsJpegAsync(rutaFisicaThumb);
            rutaThumbnailRelativa = $"/uploads/{subcarpeta}/{nombreThumb}";
        }

        return new ResultadoArchivo(rutaRelativa, rutaThumbnailRelativa);
    }
}