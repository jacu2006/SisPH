using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioMudanzas : IServicioMudanzas
{
    private readonly IRepositorio<Mudanza> _repoMudanza;
    private readonly IRepositorio<Ocupacion> _repoOcupacion;
    private readonly IRepositorio<Unidad> _repoUnidad;
    private readonly IServicioCuotas _servicioCuotas;
    private readonly SisPHDbContext _contexto;
    private readonly IServicioBitacora _bitacora;

    public ServicioMudanzas(
        IRepositorio<Mudanza> repoMudanza,
        IRepositorio<Ocupacion> repoOcupacion,
        IRepositorio<Unidad> repoUnidad,
        IServicioCuotas servicioCuotas,
        SisPHDbContext contexto,
        IServicioBitacora bitacora)
    {
        _repoMudanza = repoMudanza;
        _repoOcupacion = repoOcupacion;
        _repoUnidad = repoUnidad;
        _servicioCuotas = servicioCuotas;
        _contexto = contexto;
        _bitacora = bitacora;
    }

    public async Task<List<Mudanza>> ListarAsync(int? unidadId = null)
    {
        var query = _repoMudanza.Consultar()
            .Include(m => m.Unidad).ThenInclude(u => u!.Torre)
            .Include(m => m.Usuario)
            .AsNoTracking()
            .AsSplitQuery();

        if (unidadId.HasValue)
            query = query.Where(m => m.UnidadId == unidadId.Value);

        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    public async Task<ResultadoOperacion> RegistrarMudanzaAsync(
        int unidadId,
        TipoMudanza tipo,
        int? residenteIdEntrada,
        TipoOcupacion tipoOcupacionEntrada,
        string? observacion,
        int operadorId)
    {
        var unidad = await _repoUnidad.ObtenerPorIdAsync(unidadId);
        if (unidad is null || !unidad.Activo)
            return ResultadoOperacion.Error("La unidad seleccionada no existe o está inactiva.");

        // CU-19: Verificar paz y salvo dinámico antes de autorizar mudanza
        if (!await _servicioCuotas.UnidadEstaPazYSalvoAsync(unidadId))
            return ResultadoOperacion.Error("Mudanza bloqueada: la unidad tiene cuotas vencidas o morosas pendientes de pago.");

        await using var transaccion = await _contexto.Database.BeginTransactionAsync();
        try
        {
            if (tipo == TipoMudanza.Salida)
            {
                var vigentes = await _repoOcupacion.BuscarAsync(o => o.UnidadId == unidadId && o.FechaFin == null);
                if (vigentes.Count == 0)
                {
                    await transaccion.RollbackAsync();
                    return ResultadoOperacion.Error("La unidad no tiene una ocupación vigente para registrar salida.");
                }

                // [#7] Cierre selectivo de ocupación en salida:
                // 1) Si se indicó un residente específico, cerrar solo esa ocupación vigente.
                // 2) Si no se indicó y existen inquilinos vigentes, cerrar únicamente al inquilino manteniendo al propietario.
                // 3) Si solo existe ocupación de propietario, cerrar la más reciente.
                List<Ocupacion> aCerrar;
                if (residenteIdEntrada.HasValue)
                {
                    aCerrar = vigentes.Where(o => o.ResidenteId == residenteIdEntrada.Value).ToList();
                    if (aCerrar.Count == 0)
                    {
                        await transaccion.RollbackAsync();
                        return ResultadoOperacion.Error("El residente seleccionado no tiene una ocupación vigente en esta unidad.");
                    }
                }
                else
                {
                    var inquilinosVigentes = vigentes.Where(o => o.Tipo == TipoOcupacion.Inquilino).ToList();
                    aCerrar = inquilinosVigentes.Count > 0
                        ? inquilinosVigentes
                        : vigentes.OrderByDescending(o => o.FechaInicio).Take(1).ToList();
                }

                foreach (var oc in aCerrar)
                {
                    oc.Cerrar(DateTime.Today);
                    await _repoOcupacion.ModificarAsync(oc);
                }
            }
            else
            {
                if (!residenteIdEntrada.HasValue)
                {
                    await transaccion.RollbackAsync();
                    return ResultadoOperacion.Error("Debe seleccionar el residente que ingresa a la unidad.");
                }

                var nuevaOcupacion = new Ocupacion
                {
                    UnidadId = unidadId,
                    ResidenteId = residenteIdEntrada.Value,
                    Tipo = tipoOcupacionEntrada,
                    FechaInicio = DateTime.Today
                };
                await _repoOcupacion.AgregarAsync(nuevaOcupacion);
            }

            var mudanza = new Mudanza
            {
                UnidadId = unidadId,
                UsuarioId = operadorId,
                Tipo = tipo,
                Fecha = DateTime.Now,
                Observacion = observacion?.Trim()
            };

            await _repoMudanza.AgregarAsync(mudanza);
            await transaccion.CommitAsync();

            await _bitacora.RegistrarAsync(
                operadorId,
                "RegistrarMudanza",
                "Mudanza",
                mudanza.Id.ToString(),
                $"Mudanza de {tipo} validada para UnidadId {unidadId}.");

            return ResultadoOperacion.Ok($"Mudanza de {tipo} registrada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }
}
