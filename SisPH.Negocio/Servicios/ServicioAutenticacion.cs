using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;
using SisPH.Seguridad;

namespace SisPH.Negocio.Servicios;

public class ServicioAutenticacion : IServicioAutenticacion
{
    private const int MaxIntentos = 5;
    private const int MinutosBloqueo = 15;
    private const string MensajeGenericoError = "Credenciales inválidas o cuenta bloqueada temporalmente.";

    private readonly IRepositorio<Usuario> _repoUsuario;
    private readonly ITransformacionSegura _hash;
    private readonly IServicioBitacora _bitacora;

    public ServicioAutenticacion(
        IRepositorio<Usuario> repoUsuario,
        [FromKeyedServices("hash")] ITransformacionSegura hash,
        IServicioBitacora bitacora)
    {
        _repoUsuario = repoUsuario;
        _hash = hash;
        _bitacora = bitacora;
    }

    public async Task<ResultadoOperacion<Usuario>> LoginAsync(string correo, string clave)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(clave))
            return ResultadoOperacion<Usuario>.Error(MensajeGenericoError);

        var correoNormalizado = correo.Trim().ToLowerInvariant();

        var usuario = await _repoUsuario.Consultar()
            .Include(u => u.Rol)
            .Include(u => u.Residente)
            .FirstOrDefaultAsync(u => u.Correo.ToLower() == correoNormalizado);

        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion<Usuario>.Error(MensajeGenericoError);

        if (usuario.EstaBloqueado())
            return ResultadoOperacion<Usuario>.Error(MensajeGenericoError);

        bool claveValida = _hash.Verificar(clave, usuario.PasswordHash);

        if (!claveValida)
        {
            usuario.IntentosFallidos++;

            if (usuario.IntentosFallidos >= MaxIntentos)
            {
                usuario.Bloquear(MinutosBloqueo);
                await _repoUsuario.ModificarAsync(usuario);
                await _bitacora.RegistrarAsync(
                    usuario.Id,
                    "BloqueoCuenta",
                    "Usuario",
                    usuario.Id.ToString(),
                    $"Cuenta bloqueada {MinutosBloqueo} minutos por {MaxIntentos} intentos fallidos.");
            }
            else
            {
                await _repoUsuario.ModificarAsync(usuario);
            }

            return ResultadoOperacion<Usuario>.Error(MensajeGenericoError);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await _repoUsuario.ModificarAsync(usuario);

        await _bitacora.RegistrarAsync(
            usuario.Id,
            "Login",
            "Usuario",
            usuario.Id.ToString(),
            $"Inicio de sesión exitoso ({usuario.Rol?.Nombre}).");

        return ResultadoOperacion<Usuario>.Ok(usuario, "Bienvenido a SisPH.");
    }

    public async Task<ResultadoOperacion> CambiarPasswordAsync(int usuarioId, string claveActual, string claveNueva)
    {
        if (string.IsNullOrWhiteSpace(claveNueva) || claveNueva.Length < 8)
            return ResultadoOperacion.Error("La nueva contraseña debe tener al menos 8 caracteres.");

        var usuario = await _repoUsuario.ObtenerPorIdAsync(usuarioId);
        if (usuario is null || !usuario.Activo)
            return ResultadoOperacion.Error("Usuario no encontrado.");

        if (!_hash.Verificar(claveActual, usuario.PasswordHash))
            return ResultadoOperacion.Error("La contraseña actual es incorrecta.");

        usuario.PasswordHash = _hash.Transformar(claveNueva);
        await _repoUsuario.ModificarAsync(usuario);

        await _bitacora.RegistrarAsync(
            usuario.Id,
            "CambiarPassword",
            "Usuario",
            usuario.Id.ToString(),
            "Actualización de contraseña de acceso.");

        return ResultadoOperacion.Ok("Contraseña actualizada correctamente.");
    }
}