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

        var subcarpetaLimpia = Path.GetFileName(subcarpeta);
        var carpetaDestino = Path.Combine(rutaFisicaWwwroot, "uploads", subcarpetaLimpia);
        Directory.CreateDirectory(carpetaDestino);

        var nombreBase = Guid.NewGuid().ToString("N");
        var nombreArchivo = $"{nombreBase}{extension.ToLowerInvariant()}";
        var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);
        string? rutaFisicaThumb = null;

        try
        {
            using var imagen = await Image.LoadAsync(contenido);
            await imagen.SaveAsync(rutaFisica);

            var rutaRelativa = $"/uploads/{subcarpetaLimpia}/{nombreArchivo}";
            string? rutaThumbnailRelativa = null;

            if (generarThumbnail)
            {
                var nombreThumb = $"{nombreBase}_thumb.jpg";
                rutaFisicaThumb = Path.Combine(carpetaDestino, nombreThumb);

                imagen.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(anchoThumbnailPx, 0),
                    Mode = ResizeMode.Max
                }));

                await imagen.SaveAsJpegAsync(rutaFisicaThumb);
                rutaThumbnailRelativa = $"/uploads/{subcarpetaLimpia}/{nombreThumb}";
            }

            return new ResultadoArchivo(rutaRelativa, rutaThumbnailRelativa);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or ImageFormatException)
        {
            IntentarEliminarRutaFisica(rutaFisica);
            IntentarEliminarRutaFisica(rutaFisicaThumb);
            throw new InvalidOperationException("El archivo proporcionado no es una imagen válida o está dañado.", ex);
        }
        catch
        {
            IntentarEliminarRutaFisica(rutaFisica);
            IntentarEliminarRutaFisica(rutaFisicaThumb);
            throw;
        }
    }

    public void EliminarArchivo(string? rutaRelativa, string rutaFisicaWwwroot)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa) || string.IsNullOrWhiteSpace(rutaFisicaWwwroot))
            return;

        var relativaLimpia = rutaRelativa.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var carpetaUploads = Path.GetFullPath(Path.Combine(rutaFisicaWwwroot, "uploads"));
        var rutaCompleta = Path.GetFullPath(Path.Combine(rutaFisicaWwwroot, relativaLimpia));

        // Verificar estrictamente que la ruta resuelta esté dentro de wwwroot/uploads/
        if (!rutaCompleta.StartsWith(carpetaUploads + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        IntentarEliminarRutaFisica(rutaCompleta);
    }

    private static void IntentarEliminarRutaFisica(string? rutaFisica)
    {
        if (string.IsNullOrWhiteSpace(rutaFisica))
            return;

        try
        {
            if (File.Exists(rutaFisica))
                File.Delete(rutaFisica);
        }
        catch
        {
            // Evitar enmascarar la excepción original durante limpieza de archivos huérfanos
        }
    }
}
