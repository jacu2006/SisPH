using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioCuotas : IServicioCuotas
{
    private const decimal RecargoPorDefecto = 10.0m;

    private readonly IRepositorio<Cuota> _repoCuota;
    private readonly IRepositorio<Unidad> _repoUnidad;
    private readonly SisPHDbContext _contexto;
    private readonly IServicioBitacora _bitacora;
    private readonly IConfiguration _configuracion;

    public ServicioCuotas(
        IRepositorio<Cuota> repoCuota,
        IRepositorio<Unidad> repoUnidad,
        SisPHDbContext contexto,
        IServicioBitacora bitacora,
        IConfiguration configuracion)
    {
        _repoCuota = repoCuota;
        _repoUnidad = repoUnidad;
        _contexto = contexto;
        _bitacora = bitacora;
        _configuracion = configuracion;
    }

    public decimal ObtenerPorcentajeRecargoMora()
    {
        var valor = _configuracion["Finanzas:PorcentajeRecargoMora"];
        if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var pct) && pct >= 0m && pct <= 100m)
            return pct;

        return RecargoPorDefecto;
    }

    public async Task<List<Cuota>> ListarPorPeriodoAsync(string? periodo = null, int? torreId = null, EstadoCuota? estado = null)
    {
        var query = _repoCuota.Consultar()
            .Include(c => c.Unidad)
                .ThenInclude(u => u!.Torre)
            .Include(c => c.PagoDetalles)
            .AsNoTracking()
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(periodo))
            query = query.Where(c => c.Periodo == periodo.Trim());

        if (torreId.HasValue)
            query = query.Where(c => c.Unidad!.TorreId == torreId.Value);

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);

        return await query
            .OrderByDescending(c => c.Periodo)
            .ThenBy(c => c.Unidad!.Torre!.Nombre)
            .ThenBy(c => c.Unidad!.Numero)
            .ToListAsync();
    }

    public async Task<List<Cuota>> ListarPendientesPorUnidadAsync(int unidadId) =>
        await _repoCuota.Consultar()
            .Include(c => c.PagoDetalles)
            .AsNoTracking()
            .Where(c => c.UnidadId == unidadId && c.Estado != EstadoCuota.Pagada)
            .OrderBy(c => c.Vence)
            .ThenBy(c => c.Id)
            .ToListAsync();

    // [#6] Paz y Salvo dinámico: consulta cuotas con saldo pendiente y vencimiento anterior a hoy
    public async Task<bool> UnidadEstaPazYSalvoAsync(int unidadId)
    {
        var hoy = DateTime.Today;
        var cuotasNoPagadas = await _repoCuota.Consultar()
            .Include(c => c.PagoDetalles)
            .AsNoTracking()
            .Where(c => c.UnidadId == unidadId && c.Estado != EstadoCuota.Pagada)
            .ToListAsync();

        bool tieneAtraso = cuotasNoPagadas.Any(c =>
        {
            decimal saldo = (c.Monto + c.Recargo) - c.PagoDetalles.Sum(pd => pd.MontoAplicado);
            return saldo > 0m && (c.Vence.Date < hoy || c.Estado == EstadoCuota.Vencida || c.Estado == EstadoCuota.Morosa);
        });

        return !tieneAtraso;
    }

    public async Task<ResultadoOperacion<List<VistaPreviaCuotaItem>>> GenerarVistaPreviaAsync(
        string periodo, decimal presupuesto, TipoCuota tipo)
    {
        if (string.IsNullOrWhiteSpace(periodo) || periodo.Length != 7)
            return ResultadoOperacion<List<VistaPreviaCuotaItem>>.Error("El período debe tener formato AAAA-MM.");

        if (presupuesto <= 0)
            return ResultadoOperacion<List<VistaPreviaCuotaItem>>.Error("El presupuesto debe ser mayor a cero.");

        if (await _repoCuota.ExisteAsync(c => c.Periodo == periodo && c.Tipo == tipo))
            return ResultadoOperacion<List<VistaPreviaCuotaItem>>.Error($"Ya existen cuotas {tipo} generadas para el período {periodo}.");

        var unidades = await _repoUnidad.Consultar()
            .Include(u => u.Torre)
            .AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.Torre!.Nombre)
            .ThenBy(u => u.Numero)
            .ToListAsync();

        if (unidades.Count == 0)
            return ResultadoOperacion<List<VistaPreviaCuotaItem>>.Error("No hay unidades activas registradas.");

        // [#1] Verificar que el acumulado global de coeficientes de todo el PH no supere el 100 %
        decimal sumaCoefGlobal = unidades.Sum(u => u.Coeficiente);
        if (sumaCoefGlobal > 100m)
            return ResultadoOperacion<List<VistaPreviaCuotaItem>>.Error(
                $"La suma global de coeficientes activos del PH ({sumaCoefGlobal:0.####} %) supera el 100 %. Ajuste los coeficientes en Torres y Unidades antes de generar cuotas.");

        var lista = unidades.Select(u => new VistaPreviaCuotaItem(
            u.Id,
            u.Torre?.Nombre ?? "Sin torre",
            u.Numero,
            u.Coeficiente,
            u.CalcularCuota(presupuesto)
        )).ToList();

        return ResultadoOperacion<List<VistaPreviaCuotaItem>>.Ok(lista);
    }

    public async Task<ResultadoOperacion> GenerarCuotasAsync(
        string periodo, decimal presupuesto, TipoCuota tipo, DateTime? vence, int adminId)
    {
        var previa = await GenerarVistaPreviaAsync(periodo, presupuesto, tipo);
        if (!previa.Exito)
            return ResultadoOperacion.Error(previa.Mensaje);

        await _contexto.EjecutarGenerarCuotasAsync(periodo, presupuesto, tipo.ToString(), vence);

        await _bitacora.RegistrarAsync(
            adminId,
            "GenerarCuotas",
            "Cuota",
            periodo,
            $"Generadas {previa.Datos!.Count} cuotas ({tipo}) para período {periodo} sobre presupuesto ${presupuesto:0.00}.");

        return ResultadoOperacion.Ok($"Se generaron {previa.Datos!.Count} cuotas para el período {periodo}.");
    }

    public async Task<ResultadoOperacion> ActualizarEstadosYRecargosAsync(decimal? porcentajeRecargo = null, int? usuarioId = null)
    {
        decimal recargoAplicado = porcentajeRecargo ?? ObtenerPorcentajeRecargoMora();
        await _contexto.EjecutarActualizarEstadosCuotaAsync(recargoAplicado, DateTime.Today);

        if (usuarioId.HasValue)
        {
            await _bitacora.RegistrarAsync(
                usuarioId,
                "ActualizarEstadosCuota",
                "Cuota",
                null,
                $"Ejecución de actualización de estados y recargos ({recargoAplicado:0.##} %).");
        }

        return ResultadoOperacion.Ok($"Estados y recargos de cuotas actualizados ({recargoAplicado:0.##} %).");
    }
}
