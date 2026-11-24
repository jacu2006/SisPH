using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioUnidades : IServicioUnidades
{
    private readonly IRepositorio<Torre> _repoTorre;
    private readonly IRepositorio<Unidad> _repoUnidad;
    private readonly IServicioBitacora _bitacora;

    public ServicioUnidades(
        IRepositorio<Torre> repoTorre,
        IRepositorio<Unidad> repoUnidad,
        IServicioBitacora bitacora)
    {
        _repoTorre = repoTorre;
        _repoUnidad = repoUnidad;
        _bitacora = bitacora;
    }

    public async Task<List<Torre>> ListarTorresAsync() =>
        await _repoTorre.Consultar()
            .Include(t => t.Unidades)
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .ToListAsync();

    public async Task<ResultadoOperacion> CrearTorreAsync(string nombre, int pisos, int adminId)
    {
        if (string.IsNullOrWhiteSpace(nombre) || pisos <= 0)
            return ResultadoOperacion.Error("Debe indicar un nombre válido y al menos 1 piso.");

        var nombreNorm = nombre.Trim();
        if (await _repoTorre.ExisteAsync(t => t.Nombre.ToLower() == nombreNorm.ToLower()))
            return ResultadoOperacion.Error("Ya existe una torre con ese nombre.");

        var torre = new Torre { Nombre = nombreNorm, Pisos = pisos };
        await _repoTorre.AgregarAsync(torre);

        await _bitacora.RegistrarAsync(adminId, "Crear", "Torre", torre.Id.ToString(), $"Torre creada: {torre.Nombre} ({pisos} pisos).");
        return ResultadoOperacion.Ok("Torre registrada correctamente.");
    }

    public async Task<List<Unidad>> ListarUnidadesAsync(int? torreId = null, bool? soloActivas = null)
    {
        var query = _repoUnidad.Consultar()
            .Include(u => u.Torre)
            .Include(u => u.Ocupaciones.Where(o => o.FechaFin == null))
                .ThenInclude(o => o.Residente)
            .AsNoTracking();

        if (torreId.HasValue)
            query = query.Where(u => u.TorreId == torreId.Value);

        if (soloActivas.HasValue)
            query = query.Where(u => u.Activo == soloActivas.Value);

        return await query
            .OrderBy(u => u.Torre!.Nombre)
            .ThenBy(u => u.Piso)
            .ThenBy(u => u.Numero)
            .ToListAsync();
    }

    public async Task<Unidad?> ObtenerUnidadPorIdAsync(int id) =>
        await _repoUnidad.Consultar()
            .Include(u => u.Torre)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<decimal> ObtenerSumaCoeficientesTorreAsync(int torreId, int? excluirUnidadId = null)
    {
        var query = _repoUnidad.Consultar()
            .Where(u => u.TorreId == torreId && u.Activo);

        if (excluirUnidadId.HasValue)
            query = query.Where(u => u.Id != excluirUnidadId.Value);

        return await query.SumAsync(u => (decimal?)u.Coeficiente) ?? 0m;
    }

    public async Task<ResultadoOperacion> CrearUnidadAsync(
        int torreId, string numero, int piso, decimal metros2, decimal coeficiente, int adminId)
    {
        var torre = await _repoTorre.ObtenerPorIdAsync(torreId);
        if (torre is null)
            return ResultadoOperacion.Error("La torre seleccionada no existe.");

        var numeroNorm = numero.Trim().ToUpperInvariant();
        if (piso < 1 || piso > torre.Pisos)
            return ResultadoOperacion.Error($"El piso debe estar entre 1 y {torre.Pisos} para {torre.Nombre}.");

        if (metros2 <= 0 || coeficiente <= 0)
            return ResultadoOperacion.Error("Los metros cuadrados y el coeficiente deben ser mayores a cero.");

        if (await _repoUnidad.ExisteAsync(u => u.TorreId == torreId && u.Numero.ToUpper() == numeroNorm))
            return ResultadoOperacion.Error($"Ya existe la unidad {numeroNorm} en {torre.Nombre}.");

        var sumaActual = await ObtenerSumaCoeficientesTorreAsync(torreId);
        if (sumaActual + coeficiente > 100m)
            return ResultadoOperacion.Error(
                $"La suma de coeficientes de {torre.Nombre} superaría el 100 % (actual: {sumaActual:0.####} %, disponible: {100m - sumaActual:0.####} %).");

        var unidad = new Unidad
        {
            TorreId = torreId,
            Numero = numeroNorm,
            Piso = piso,
            Metros2 = metros2,
            Coeficiente = coeficiente,
            Activo = true
        };

        await _repoUnidad.AgregarAsync(unidad);

        await _bitacora.RegistrarAsync(
            adminId,
            "Crear",
            "Unidad",
            unidad.Id.ToString(),
            $"Unidad {numeroNorm} creada en {torre.Nombre} (Coef: {coeficiente} %).");

        return ResultadoOperacion.Ok("Unidad registrada correctamente.");
    }

    public async Task<ResultadoOperacion> ModificarUnidadAsync(
        int id, int torreId, string numero, int piso, decimal metros2, decimal coeficiente, int adminId)
    {
        var unidad = await _repoUnidad.ObtenerPorIdAsync(id);
        if (unidad is null)
            return ResultadoOperacion.Error("La unidad no existe.");

        var torre = await _repoTorre.ObtenerPorIdAsync(torreId);
        if (torre is null)
            return ResultadoOperacion.Error("La torre seleccionada no existe.");

        var numeroNorm = numero.Trim().ToUpperInvariant();
        if (piso < 1 || piso > torre.Pisos)
            return ResultadoOperacion.Error($"El piso debe estar entre 1 y {torre.Pisos}.");

        if (await _repoUnidad.ExisteAsync(u => u.Id != id && u.TorreId == torreId && u.Numero.ToUpper() == numeroNorm))
            return ResultadoOperacion.Error($"Ya existe otra unidad con el número {numeroNorm} en {torre.Nombre}.");

        if (unidad.Activo)
        {
            var sumaActual = await ObtenerSumaCoeficientesTorreAsync(torreId, id);
            if (sumaActual + coeficiente > 100m)
                return ResultadoOperacion.Error(
                    $"La suma de coeficientes superaría el 100 % (disponible: {100m - sumaActual:0.####} %).");
        }

        unidad.TorreId = torreId;
        unidad.Numero = numeroNorm;
        unidad.Piso = piso;
        unidad.Metros2 = metros2;
        unidad.Coeficiente = coeficiente;

        await _repoUnidad.ModificarAsync(unidad);

        await _bitacora.RegistrarAsync(
            adminId,
            "Modificar",
            "Unidad",
            unidad.Id.ToString(),
            $"Unidad {numeroNorm} modificada en {torre.Nombre}.");

        return ResultadoOperacion.Ok("Unidad actualizada correctamente.");
    }

    public async Task<ResultadoOperacion> CambiarEstadoUnidadAsync(int id, bool activo, int adminId)
    {
        var unidad = await _repoUnidad.ObtenerPorIdAsync(id);
        if (unidad is null)
            return ResultadoOperacion.Error("La unidad no existe.");

        if (activo)
        {
            var sumaActual = await ObtenerSumaCoeficientesTorreAsync(unidad.TorreId, id);
            if (sumaActual + unidad.Coeficiente > 100m)
                return ResultadoOperacion.Error("No se puede reactivar porque la suma de coeficientes de la torre superaría el 100 %.");
            unidad.Activo = true;
        }
        else
        {
            unidad.DarDeBaja();
        }

        await _repoUnidad.ModificarAsync(unidad);

        await _bitacora.RegistrarAsync(
            adminId,
            activo ? "Reactivar" : "BajaLogica",
            "Unidad",
            unidad.Id.ToString(),
            $"Unidad {unidad.Numero} Activo = {activo}.");

        return ResultadoOperacion.Ok(activo ? "Unidad reactivada." : "Unidad desactivada.");
    }
}