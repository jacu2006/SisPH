using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;
using SisPH.Seguridad;

namespace SisPH.Negocio.Servicios;

public class ServicioResidentes : IServicioResidentes
{
    private readonly IRepositorio<Residente> _repoResidente;
    private readonly IRepositorio<Ocupacion> _repoOcupacion;
    private readonly IRepositorio<Unidad> _repoUnidad;
    private readonly IRepositorio<Usuario> _repoUsuario;
    private readonly IServicioArchivos _archivos;
    private readonly ITransformacionSegura _hash;
    private readonly IServicioBitacora _bitacora;

    public ServicioResidentes(
        IRepositorio<Residente> repoResidente,
        IRepositorio<Ocupacion> repoOcupacion,
        IRepositorio<Unidad> repoUnidad,
        IRepositorio<Usuario> repoUsuario,
        IServicioArchivos archivos,
        [FromKeyedServices("hash")] ITransformacionSegura hash,
        IServicioBitacora bitacora)
    {
        _repoResidente = repoResidente;
        _repoOcupacion = repoOcupacion;
        _repoUnidad = repoUnidad;
        _repoUsuario = repoUsuario;
        _archivos = archivos;
        _hash = hash;
        _bitacora = bitacora;
    }

    public async Task<List<Residente>> ListarAsync(bool? soloActivos = null)
    {
        var query = _repoResidente.Consultar()
            .Include(r => r.Usuario)
            .Include(r => r.Ocupaciones.Where(o => o.FechaFin == null))
                .ThenInclude(o => o.Unidad)
                    .ThenInclude(u => u!.Torre)
            .AsNoTracking();

        if (soloActivos.HasValue)
            query = query.Where(r => r.Activo == soloActivos.Value);

        return await query
            .OrderBy(r => r.Apellido)
            .ThenBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<Residente?> ObtenerPorIdAsync(int id) =>
        await _repoResidente.Consultar()
            .Include(r => r.Usuario)
            .Include(r => r.Ocupaciones)
                .ThenInclude(o => o.Unidad)
                    .ThenInclude(u => u!.Torre)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<ResultadoOperacion<string?>> CrearAsync(
        string cedula,
        string nombre,
        string apellido,
        string? telefono,
        string correo,
        int unidadId,
        TipoOcupacion tipoOcupacion,
        bool crearUsuarioPortal,
        ArchivoSubido? foto,
        string rutaFisicaWwwroot,
        int adminId)
    {
        var cedulaNorm = cedula.Trim();
        var correoNorm = correo.Trim().ToLowerInvariant();

        if (await _repoResidente.ExisteAsync(r => r.Cedula == cedulaNorm))
            return ResultadoOperacion<string?>.Error("Ya existe un residente registrado con esa cédula.");

        if (await _repoResidente.ExisteAsync(r => r.Correo.ToLower() == correoNorm))
            return ResultadoOperacion<string?>.Error("Ya existe un residente registrado con ese correo.");

        if (crearUsuarioPortal && await _repoUsuario.ExisteAsync(u => u.Correo.ToLower() == correoNorm))
            return ResultadoOperacion<string?>.Error("El correo ya está en uso por otra cuenta de usuario del sistema.");

        var unidad = await _repoUnidad.ObtenerPorIdAsync(unidadId);
        if (unidad is null || !unidad.Activo)
            return ResultadoOperacion<string?>.Error("La unidad seleccionada no existe o está inactiva.");

        string? rutaFoto = null;
        if (foto is not null && foto.TamanioBytes > 0)
        {
            try
            {
                var guardado = await _archivos.GuardarImagenAsync(
                    foto.Contenido,
                    foto.NombreOriginal,
                    foto.TamanioBytes,
                    "residentes",
                    rutaFisicaWwwroot,
                    generarThumbnail: false);
                rutaFoto = guardado.RutaRelativa;
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion<string?>.Error(ex.Message);
            }
        }

        int? usuarioId = null;
        string? claveTemporal = null;

        if (crearUsuarioPortal)
        {
            claveTemporal = $"Res#{Guid.NewGuid().ToString("N")[..6]}";
            var usuarioPortal = new Usuario
            {
                RolId = (int)SisPH.Entidades.Enums.Rol.Residente,
                Nombre = $"{nombre.Trim()} {apellido.Trim()}",
                Correo = correoNorm,
                PasswordHash = _hash.Transformar(claveTemporal),
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            await _repoUsuario.AgregarAsync(usuarioPortal);
            usuarioId = usuarioPortal.Id;
        }

        var residente = new Residente
        {
            UsuarioId = usuarioId,
            Cedula = cedulaNorm,
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Foto = rutaFoto,
            Telefono = telefono?.Trim(),
            Correo = correoNorm,
            Activo = true
        };

        await _repoResidente.AgregarAsync(residente);

        var ocupacion = new Ocupacion
        {
            UnidadId = unidadId,
            ResidenteId = residente.Id,
            Tipo = tipoOcupacion,
            FechaInicio = DateTime.Today
        };

        await _repoOcupacion.AgregarAsync(ocupacion);

        await _bitacora.RegistrarAsync(
            adminId,
            "Crear",
            "Residente",
            residente.Id.ToString(),
            $"Residente {residente.NombreCompleto()} ({cedulaNorm}) asignado a UnidadId {unidadId} como {tipoOcupacion}.");

        var msg = claveTemporal is null
            ? "Residente registrado y asignado correctamente."
            : $"Residente registrado con acceso al portal. Clave temporal: {claveTemporal}";

        return ResultadoOperacion<string?>.Ok(claveTemporal, msg);
    }

    public async Task<ResultadoOperacion> ModificarAsync(
        int id,
        string cedula,
        string nombre,
        string apellido,
        string? telefono,
        string correo,
        ArchivoSubido? nuevaFoto,
        string rutaFisicaWwwroot,
        int adminId)
    {
        var residente = await _repoResidente.ObtenerPorIdAsync(id);
        if (residente is null)
            return ResultadoOperacion.Error("El residente no existe.");

        var cedulaNorm = cedula.Trim();
        var correoNorm = correo.Trim().ToLowerInvariant();

        if (await _repoResidente.ExisteAsync(r => r.Id != id && r.Cedula == cedulaNorm))
            return ResultadoOperacion.Error("La cédula ya pertenece a otro residente.");

        if (await _repoResidente.ExisteAsync(r => r.Id != id && r.Correo.ToLower() == correoNorm))
            return ResultadoOperacion.Error("El correo ya pertenece a otro residente.");

        if (nuevaFoto is not null && nuevaFoto.TamanioBytes > 0)
        {
            try
            {
                var guardado = await _archivos.GuardarImagenAsync(
                    nuevaFoto.Contenido,
                    nuevaFoto.NombreOriginal,
                    nuevaFoto.TamanioBytes,
                    "residentes",
                    rutaFisicaWwwroot,
                    generarThumbnail: false);
                residente.Foto = guardado.RutaRelativa;
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        residente.Cedula = cedulaNorm;
        residente.Nombre = nombre.Trim();
        residente.Apellido = apellido.Trim();
        residente.Telefono = telefono?.Trim();
        residente.Correo = correoNorm;

        await _repoResidente.ModificarAsync(residente);

        await _bitacora.RegistrarAsync(
            adminId,
            "Modificar",
            "Residente",
            residente.Id.ToString(),
            $"Datos actualizados de {residente.NombreCompleto()}.");

        return ResultadoOperacion.Ok("Residente actualizado correctamente.");
    }

    public async Task<ResultadoOperacion> DarDeBajaAsync(int id, int adminId)
    {
        var residente = await ObtenerPorIdAsync(id);
        if (residente is null)
            return ResultadoOperacion.Error("El residente no existe.");

        residente.DarDeBaja();
        await _repoResidente.ModificarAsync(residente);

        foreach (var ocupacion in residente.Ocupaciones.Where(o => o.EstaVigente()))
        {
            ocupacion.Cerrar(DateTime.Today);
            await _repoOcupacion.ModificarAsync(ocupacion);
        }

        if (residente.Usuario is not null && residente.Usuario.Activo)
        {
            residente.Usuario.DarDeBaja();
            await _repoUsuario.ModificarAsync(residente.Usuario);
        }

        await _bitacora.RegistrarAsync(
            adminId,
            "BajaLogica",
            "Residente",
            residente.Id.ToString(),
            $"Baja lógica de residente {residente.NombreCompleto()}.");

        return ResultadoOperacion.Ok("Residente dado de baja correctamente.");
    }
}