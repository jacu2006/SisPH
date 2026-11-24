using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;
using SisPH.Seguridad;

namespace SisPH.Negocio.Servicios;

public class ServicioUsuarios : IServicioUsuarios
{
    private readonly IRepositorio<Usuario> _repoUsuario;
    private readonly IRepositorio<Rol> _repoRol;
    private readonly ITransformacionSegura _hash;
    private readonly IServicioBitacora _bitacora;

    public ServicioUsuarios(
        IRepositorio<Usuario> repoUsuario,
        IRepositorio<Rol> repoRol,
        [FromKeyedServices("hash")] ITransformacionSegura hash,
        IServicioBitacora bitacora)
    {
        _repoUsuario = repoUsuario;
        _repoRol = repoRol;
        _hash = hash;
        _bitacora = bitacora;
    }

    public async Task<List<Usuario>> ListarAsync(bool? soloActivos = null)
    {
        var query = _repoUsuario.Consultar()
            .Include(u => u.Rol)
            .AsNoTracking();

        if (soloActivos.HasValue)
            query = query.Where(u => u.Activo == soloActivos.Value);

        return await query.OrderBy(u => u.Nombre).ToListAsync();
    }

    public async Task<List<Rol>> ListarRolesAsync() =>
        await _repoRol.Consultar().AsNoTracking().OrderBy(r => r.Id).ToListAsync();

    public async Task<Usuario?> ObtenerPorIdAsync(int id) =>
        await _repoUsuario.Consultar()
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<ResultadoOperacion<string>> CrearAsync(string nombre, string correo, int rolId, int adminId)
    {
        var correoNorm = correo.Trim().ToLowerInvariant();
        if (await _repoUsuario.ExisteAsync(u => u.Correo.ToLower() == correoNorm))
            return ResultadoOperacion<string>.Error("Ya existe un usuario registrado con ese correo electrónico.");

        var claveTemporal = $"SisPH#{Guid.NewGuid().ToString("N")[..6]}";

        var usuario = new Usuario
        {
            Nombre = nombre.Trim(),
            Correo = correoNorm,
            RolId = rolId,
            PasswordHash = _hash.Transformar(claveTemporal),
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        await _repoUsuario.AgregarAsync(usuario);

        await _bitacora.RegistrarAsync(
            adminId,
            "Crear",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario creado: {usuario.Correo} (RolId: {rolId}).");

        return ResultadoOperacion<string>.Ok(
            claveTemporal,
            $"Usuario creado correctamente. Contraseña temporal: {claveTemporal}");
    }

    public async Task<ResultadoOperacion> ModificarAsync(int id, string nombre, string correo, int rolId, int adminId)
    {
        var usuario = await _repoUsuario.ObtenerPorIdAsync(id);
        if (usuario is null)
            return ResultadoOperacion.Error("El usuario no existe.");

        var correoNorm = correo.Trim().ToLowerInvariant();
        if (await _repoUsuario.ExisteAsync(u => u.Id != id && u.Correo.ToLower() == correoNorm))
            return ResultadoOperacion.Error("El correo ingresado ya pertenece a otro usuario.");

        usuario.Nombre = nombre.Trim();
        usuario.Correo = correoNorm;
        usuario.RolId = rolId;

        await _repoUsuario.ModificarAsync(usuario);

        await _bitacora.RegistrarAsync(
            adminId,
            "Modificar",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario actualizado: {usuario.Correo} (RolId: {rolId}).");

        return ResultadoOperacion.Ok("Usuario actualizado correctamente.");
    }

    public async Task<ResultadoOperacion> CambiarEstadoActivoAsync(int id, bool activo, int adminId)
    {
        if (!activo && id == adminId)
            return ResultadoOperacion.Error("No puedes darte de baja a ti mismo.");

        var usuario = await _repoUsuario.ObtenerPorIdAsync(id);
        if (usuario is null)
            return ResultadoOperacion.Error("El usuario no existe.");

        if (!activo)
            usuario.DarDeBaja();
        else
        {
            usuario.Activo = true;
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
        }

        await _repoUsuario.ModificarAsync(usuario);

        await _bitacora.RegistrarAsync(
            adminId,
            activo ? "Reactivar" : "BajaLogica",
            "Usuario",
            usuario.Id.ToString(),
            $"Estado Activo cambiado a {activo} para {usuario.Correo}.");

        return ResultadoOperacion.Ok(activo ? "Usuario reactivado." : "Usuario dado de baja.");
    }
}