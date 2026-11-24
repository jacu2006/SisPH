using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;

namespace SisPH.Negocio.Servicios;

public class ServicioCuenta : IServicioCuenta
{
    private readonly IRepositorio<Residente> _repoResidente;
    private readonly IRepositorio<Cuota> _repoCuota;
    private readonly IRepositorio<Pago> _repoPago;
    private readonly IRepositorio<Reserva> _repoReserva;
    private readonly IRepositorio<Incidencia> _repoIncidencia;
    private readonly IRepositorio<PaseVisita> _repoPase;

    public ServicioCuenta(
        IRepositorio<Residente> repoResidente,
        IRepositorio<Cuota> repoCuota,
        IRepositorio<Pago> repoPago,
        IRepositorio<Reserva> repoReserva,
        IRepositorio<Incidencia> repoIncidencia,
        IRepositorio<PaseVisita> repoPase)
    {
        _repoResidente = repoResidente;
        _repoCuota = repoCuota;
        _repoPago = repoPago;
        _repoReserva = repoReserva;
        _repoIncidencia = repoIncidencia;
        _repoPase = repoPase;
    }

    public async Task<EstadoCuentaResidenteDto?> ObtenerPortalResidenteAsync(int usuarioId)
    {
        var residente = await _repoResidente.Consultar()
            .Include(r => r.Ocupaciones)
                .ThenInclude(o => o.Unidad)
                    .ThenInclude(u => u!.Torre)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UsuarioId == usuarioId && r.Activo);

        if (residente is null)
            return null;

        var ocupacionVigente = residente.Ocupaciones
            .Where(o => o.FechaFin == null)
            .OrderByDescending(o => o.FechaInicio)
            .FirstOrDefault();

        var unidad = ocupacionVigente?.Unidad;

        var cuotas = unidad is null
            ? new List<Cuota>()
            : await _repoCuota.Consultar()
                .Include(c => c.PagoDetalles)
                .Where(c => c.UnidadId == unidad.Id)
                .OrderByDescending(c => c.Periodo)
                .AsNoTracking()
                .ToListAsync();

        var pagos = unidad is null
            ? new List<Pago>()
            : await _repoPago.Consultar()
                .Include(p => p.Detalles).ThenInclude(d => d.Cuota)
                .Where(p => p.UnidadId == unidad.Id)
                .OrderByDescending(p => p.Fecha)
                .AsNoTracking()
                .ToListAsync();

        var reservas = await _repoReserva.Consultar()
            .Include(r => r.AreaSocial)
            .Where(r => r.ResidenteId == residente.Id)
            .OrderByDescending(r => r.Fecha)
            .AsNoTracking()
            .ToListAsync();

        var incidencias = await _repoIncidencia.Consultar()
            .Where(i => i.ResidenteId == residente.Id)
            .OrderByDescending(i => i.Fecha)
            .AsNoTracking()
            .ToListAsync();

        var pases = await _repoPase.Consultar()
            .Where(p => p.ResidenteId == residente.Id)
            .OrderByDescending(p => p.VigenteHasta)
            .AsNoTracking()
            .ToListAsync();

        bool pazYSalvo = !cuotas.Any(c => c.Estado == EstadoCuota.Vencida || c.Estado == EstadoCuota.Morosa);
        decimal totalAdeudado = cuotas
            .Where(c => c.Estado != EstadoCuota.Pagada)
            .Sum(c => (c.Monto + c.Recargo) - c.PagoDetalles.Sum(pd => pd.MontoAplicado));

        return new EstadoCuentaResidenteDto(
            residente,
            unidad,
            pazYSalvo,
            totalAdeudado,
            cuotas,
            pagos,
            reservas,
            incidencias,
            pases);
    }
}