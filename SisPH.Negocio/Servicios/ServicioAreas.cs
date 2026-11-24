using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioAreas : IServicioAreas
{
    private readonly IRepositorio<AreaSocial> _repoArea;
    private readonly IRepositorio<ImagenArea> _repoImagen;
    private readonly IServicioArchivos _archivos;
    private readonly IServicioBitacora _bitacora;

    public ServicioAreas(
        IRepositorio<AreaSocial> repoArea,
        IRepositorio<ImagenArea> repoImagen,
        IServicioArchivos archivos,
        IServicioBitacora bitacora)
    {
        _repoArea = repoArea;
        _repoImagen = repoImagen;
        _archivos = archivos;
        _bitacora = bitacora;
    }

    public async Task<List<AreaSocial>> ListarAsync(bool? soloActivas = null)
    {
        var query = _repoArea.Consultar()
            .Include(a => a.Imagenes)
            .AsNoTracking();

        if (soloActivas.HasValue)
            query = query.Where(a => a.Activo == soloActivas.Value);

        return await query.OrderBy(a => a.Nombre).ToListAsync();
    }

    public async Task<AreaSocial?> ObtenerPorIdAsync(int id) =>
        await _repoArea.Consultar()
            .Include(a => a.Imagenes)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<ResultadoOperacion> CrearAsync(
        string nombre,
        string? descripcion,
        int capacidad,
        decimal costo,
        decimal deposito,
        TimeSpan horaApertura,
        TimeSpan horaCierre,
        List<ArchivoSubido> imagenes,
        int indicePrincipal,
        string rutaFisicaWwwroot,
        int adminId)
    {
        if (horaCierre <= horaApertura)
            return ResultadoOperacion.Error("La hora de cierre debe ser posterior a la hora de apertura.");

        if (capacidad <= 0 || costo < 0 || deposito < 0)
            return ResultadoOperacion.Error("La capacidad debe ser mayor a cero y los montos no pueden ser negativos.");

        if (imagenes is null || imagenes.Count < 2)
            return ResultadoOperacion.Error("Debe subir al menos 2 imágenes para registrar un área social.");

        var nombreNorm = nombre.Trim();
        if (await _repoArea.ExisteAsync(a => a.Nombre.ToLower() == nombreNorm.ToLower()))
            return ResultadoOperacion.Error("Ya existe un área social con ese nombre.");

        if (indicePrincipal < 0 || indicePrincipal >= imagenes.Count)
            indicePrincipal = 0;

        var resultadosArchivos = new List<ResultadoArchivo>();
        try
        {
            foreach (var img in imagenes)
            {
                var guardada = await _archivos.GuardarImagenAsync(
                    img.Contenido,
                    img.NombreOriginal,
                    img.TamanioBytes,
                    "areas",
                    rutaFisicaWwwroot,
                    generarThumbnail: true,
                    anchoThumbnailPx: 200);
                resultadosArchivos.Add(guardada);
            }
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion.Error(ex.Message);
        }

        var area = new AreaSocial
        {
            Nombre = nombreNorm,
            Descripcion = descripcion?.Trim(),
            Capacidad = capacidad,
            Costo = costo,
            Deposito = deposito,
            HoraApertura = horaApertura,
            HoraCierre = horaCierre,
            Activo = true
        };

        await _repoArea.AgregarAsync(area);

        var entidadesImagenes = new List<ImagenArea>();
        for (int i = 0; i < resultadosArchivos.Count; i++)
        {
            entidadesImagenes.Add(new ImagenArea
            {
                AreaId = area.Id,
                Ruta = resultadosArchivos[i].RutaRelativa,
                RutaThumbnail = resultadosArchivos[i].RutaThumbnailRelativa ?? resultadosArchivos[i].RutaRelativa,
                EsPrincipal = (i == indicePrincipal)
            });
        }

        await _repoImagen.AgregarVariasAsync(entidadesImagenes);

        await _bitacora.RegistrarAsync(
            adminId,
            "Crear",
            "AreaSocial",
            area.Id.ToString(),
            $"Área social creada: {area.Nombre} con {entidadesImagenes.Count} imágenes.");

        return ResultadoOperacion.Ok("Área social registrada correctamente.");
    }

    public async Task<ResultadoOperacion> ModificarAsync(
        int id,
        string nombre,
        string? descripcion,
        int capacidad,
        decimal costo,
        decimal deposito,
        TimeSpan horaApertura,
        TimeSpan horaCierre,
        List<ArchivoSubido>? nuevasImagenes,
        int? imagenPrincipalId,
        string rutaFisicaWwwroot,
        int adminId)
    {
        var area = await ObtenerPorIdAsync(id);
        if (area is null)
            return ResultadoOperacion.Error("El área social no existe.");

        if (horaCierre <= horaApertura)
            return ResultadoOperacion.Error("La hora de cierre debe ser posterior a la hora de apertura.");

        var nombreNorm = nombre.Trim();
        if (await _repoArea.ExisteAsync(a => a.Id != id && a.Nombre.ToLower() == nombreNorm.ToLower()))
            return ResultadoOperacion.Error("Ya existe otra área social con ese nombre.");

        area.Nombre = nombreNorm;
        area.Descripcion = descripcion?.Trim();
        area.Capacidad = capacidad;
        area.Costo = costo;
        area.Deposito = deposito;
        area.HoraApertura = horaApertura;
        area.HoraCierre = horaCierre;

        await _repoArea.ModificarAsync(area);

        if (imagenPrincipalId.HasValue && area.Imagenes.Any(i => i.Id == imagenPrincipalId.Value))
        {
            foreach (var img in area.Imagenes)
            {
                bool debeSerPrincipal = img.Id == imagenPrincipalId.Value;
                if (img.EsPrincipal != debeSerPrincipal)
                {
                    img.EsPrincipal = debeSerPrincipal;
                    await _repoImagen.ModificarAsync(img);
                }
            }
        }

        if (nuevasImagenes is not null && nuevasImagenes.Count > 0)
        {
            try
            {
                foreach (var img in nuevasImagenes)
                {
                    var guardada = await _archivos.GuardarImagenAsync(
                        img.Contenido,
                        img.NombreOriginal,
                        img.TamanioBytes,
                        "areas",
                        rutaFisicaWwwroot,
                        generarThumbnail: true,
                        anchoThumbnailPx: 200);

                    await _repoImagen.AgregarAsync(new ImagenArea
                    {
                        AreaId = area.Id,
                        Ruta = guardada.RutaRelativa,
                        RutaThumbnail = guardada.RutaThumbnailRelativa ?? guardada.RutaRelativa,
                        EsPrincipal = false
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        await _bitacora.RegistrarAsync(
            adminId,
            "Modificar",
            "AreaSocial",
            area.Id.ToString(),
            $"Área social modificada: {area.Nombre}.");

        return ResultadoOperacion.Ok("Área social actualizada correctamente.");
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo, int adminId)
    {
        var area = await _repoArea.ObtenerPorIdAsync(id);
        if (area is null)
            return ResultadoOperacion.Error("El área social no existe.");

        if (!activo)
            area.DarDeBaja();
        else
            area.Activo = true;

        await _repoArea.ModificarAsync(area);

        await _bitacora.RegistrarAsync(
            adminId,
            activo ? "Reactivar" : "BajaLogica",
            "AreaSocial",
            area.Id.ToString(),
            $"Área social {area.Nombre} Activo = {activo}.");

        return ResultadoOperacion.Ok(activo ? "Área social activada." : "Área social desactivada.");
    }
}