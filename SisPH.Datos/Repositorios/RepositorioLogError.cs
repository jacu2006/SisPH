using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;

namespace SisPH.Datos.Repositorios;

public class RepositorioLogError : IRepositorioLogError
{
    private readonly SisPHDbContext _contexto;

    public RepositorioLogError(SisPHDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<long> GuardarAsync(Exception ex, string origen, string? ruta, string? ip, int? usuarioId)
    {
        var mensaje = ex.InnerException?.Message ?? ex.Message;
        if (mensaje.Length > 1000)
            mensaje = mensaje[..1000];

        var log = new LogError
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.Now,
            Origen = origen.Length > 200 ? origen[..200] : origen,
            Mensaje = mensaje,
            StackTrace = ex.ToString(),
            Ruta = ruta != null && ruta.Length > 300 ? ruta[..300] : ruta,
            Ip = ip != null && ip.Length > 45 ? ip[..45] : ip
        };

        _contexto.ChangeTracker.Clear();

        await _contexto.LogErrores.AddAsync(log);
        await _contexto.SaveChangesAsync();
        return log.Id;
    }
}