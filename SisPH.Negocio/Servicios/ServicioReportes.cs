using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.DTOs;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioReportes : IServicioReportes
{
    private readonly SisPHDbContext _contexto;
    private readonly IRepositorio<Pago> _repoPago;
    private readonly IRepositorio<Reserva> _repoReserva;
    private readonly IRepositorio<Presupuesto> _repoPresupuesto;
    private readonly IExportadorExcel _excel;
    private readonly IServicioBitacora _bitacora;

    public ServicioReportes(
        SisPHDbContext contexto,
        IRepositorio<Pago> repoPago,
        IRepositorio<Reserva> repoReserva,
        IRepositorio<Presupuesto> repoPresupuesto,
        IExportadorExcel excel,
        IServicioBitacora bitacora)
    {
        _contexto = contexto;
        _repoPago = repoPago;
        _repoReserva = repoReserva;
        _repoPresupuesto = repoPresupuesto;
        _excel = excel;
        _bitacora = bitacora;
    }

    public async Task<List<MorosidadView>> ObtenerMorosidadAsync(int? torreId = null, bool soloDosOMasMeses = false)
    {
        var query = _contexto.Morosidad.AsNoTracking();

        if (torreId.HasValue)
            query = query.Where(m => m.TorreId == torreId.Value);

        if (soloDosOMasMeses)
            query = query.Where(m => m.CuotasMorosas > 0 || m.TotalCuotasAtrasadas >= 2);

        return await query
            .OrderByDescending(m => m.SaldoAdeudado)
            .ToListAsync();
    }

    public async Task<List<IngresoMensualDto>> ObtenerIngresosPorMesAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        var query = _repoPago.Consultar().AsNoTracking();

        if (desde.HasValue)
            query = query.Where(p => p.Fecha >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(p => p.Fecha < hasta.Value.Date.AddDays(1));

        var pagos = await query.ToListAsync();

        return pagos
            .GroupBy(p => p.Fecha.ToString("yyyy-MM"))
            .OrderByDescending(g => g.Key)
            .Select(g => new IngresoMensualDto(g.Key, g.Count(), g.Sum(x => x.Monto)))
            .ToList();
    }

    public async Task<List<UsoAreaSocialDto>> ObtenerUsoAreasSocialesAsync(int? areaId = null, DateTime? desde = null, DateTime? hasta = null)
    {
        var query = _repoReserva.Consultar()
            .Include(r => r.AreaSocial)
            .AsNoTracking();

        if (areaId.HasValue)
            query = query.Where(r => r.AreaId == areaId.Value);

        if (desde.HasValue)
            query = query.Where(r => r.Fecha >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(r => r.Fecha <= hasta.Value.Date);

        var reservas = await query.ToListAsync();

        return reservas
            .GroupBy(r => r.AreaSocial?.Nombre ?? "Sin área")
            .Select(g => new UsoAreaSocialDto(
                g.Key,
                g.Count(),
                g.Count(r => r.Estado == EstadoReserva.Aprobada || r.Estado == EstadoReserva.Usada),
                g.Where(r => r.Estado == EstadoReserva.Aprobada || r.Estado == EstadoReserva.Usada)
                 .Sum(r => r.AreaSocial?.Costo ?? 0m)))
            .OrderByDescending(x => x.AprobadasOUsadas)
            .ToList();
    }

    public async Task<byte[]> ExportarMorosidadExcelAsync(int? torreId, bool soloDosOMasMeses, int adminId)
    {
        var datos = await ObtenerMorosidadAsync(torreId, soloDosOMasMeses);
        var bytes = _excel.ExportarTabla(
            "Morosidad",
            "Reporte de Morosidad por Torre",
            datos,
            new Dictionary<string, Func<MorosidadView, object?>>
            {
                ["Torre"] = m => m.Torre,
                ["Unidad"] = m => m.NumeroUnidad,
                ["Residente"] = m => m.Residente,
                ["Cédula"] = m => m.Cedula,
                ["Cuotas Vencidas"] = m => m.CuotasVencidas,
                ["Cuotas Morosas (2+ m)"] = m => m.CuotasMorosas,
                ["Saldo Adeudado ($)"] = m => m.SaldoAdeudado
            });

        await _bitacora.RegistrarAsync(adminId, "ExportarExcel", "ReporteMorosidad", null, $"Exportadas {datos.Count} filas.");
        return bytes;
    }

    public async Task<byte[]> ExportarIngresosExcelAsync(DateTime? desde, DateTime? hasta, int adminId)
    {
        var datos = await ObtenerIngresosPorMesAsync(desde, hasta);
        var bytes = _excel.ExportarTabla(
            "IngresosMensuales",
            "Reporte de Ingresos por Mes",
            datos,
            new Dictionary<string, Func<IngresoMensualDto, object?>>
            {
                ["Período (AAAA-MM)"] = i => i.Periodo,
                ["Cantidad de Pagos"] = i => i.CantidadPagos,
                ["Total Recaudado ($)"] = i => i.TotalRecaudado
            });

        await _bitacora.RegistrarAsync(adminId, "ExportarExcel", "ReporteIngresos", null, $"Exportados {datos.Count} períodos.");
        return bytes;
    }

    public async Task<byte[]> ExportarUsoAreasExcelAsync(int? areaId, DateTime? desde, DateTime? hasta, int adminId)
    {
        var datos = await ObtenerUsoAreasSocialesAsync(areaId, desde, hasta);
        var bytes = _excel.ExportarTabla(
            "UsoAreasSociales",
            "Reporte de Uso de Áreas Sociales",
            datos,
            new Dictionary<string, Func<UsoAreaSocialDto, object?>>
            {
                ["Área Social"] = u => u.AreaSocial,
                ["Solicitudes Totales"] = u => u.TotalReservas,
                ["Aprobadas / Usadas"] = u => u.AprobadasOUsadas,
                ["Ingresos Estimados ($)"] = u => u.IngresosEstimados
            });

        await _bitacora.RegistrarAsync(adminId, "ExportarExcel", "ReporteUsoAreas", null, $"Exportadas {datos.Count} áreas.");
        return bytes;
    }

    public async Task<List<Presupuesto>> ListarPresupuestoPorAnioAsync(int anio) =>
        await _repoPresupuesto.Consultar()
            .Where(p => p.Anio == anio)
            .OrderBy(p => p.Tipo)
            .ThenBy(p => p.Concepto)
            .AsNoTracking()
            .ToListAsync();

    public async Task<ResultadoOperacion> GuardarLineaPresupuestoAsync(
        int anio, string concepto, string tipo, decimal montoProyectado, int adminId)
    {
        if (anio < 2000 || string.IsNullOrWhiteSpace(concepto) || montoProyectado < 0)
            return ResultadoOperacion.Error("Verifique el año, concepto y monto proyectado.");

        if (tipo != "Ingreso" && tipo != "Gasto")
            return ResultadoOperacion.Error("El tipo debe ser 'Ingreso' o 'Gasto'.");

        var linea = new Presupuesto
        {
            Anio = anio,
            Concepto = concepto.Trim(),
            Tipo = tipo,
            MontoProyectado = montoProyectado
        };

        await _repoPresupuesto.AgregarAsync(linea);

        await _bitacora.RegistrarAsync(
            adminId,
            "RegistrarPresupuesto",
            "Presupuesto",
            linea.Id.ToString(),
            $"Presupuesto {anio}: {tipo} - {linea.Concepto} (${montoProyectado:0.00}).");

        return ResultadoOperacion.Ok("Línea de presupuesto registrada.");
    }

    public async Task<List<ProyeccionAnualDto>> CalcularProyeccionQuinquenalAsync(int anioBase, decimal tasaIncrementoAnualPct = 3.0m)
    {
        var lineasBase = await ListarPresupuestoPorAnioAsync(anioBase);
        decimal ingresosBase = lineasBase.Where(l => l.Tipo == "Ingreso").Sum(l => l.MontoProyectado);
        decimal gastosBase = lineasBase.Where(l => l.Tipo == "Gasto").Sum(l => l.MontoProyectado);

        var proyeccion = new List<ProyeccionAnualDto>();
        for (int i = 0; i < 5; i++)
        {
            decimal factor = (decimal)Math.Pow(1.0 + (double)(tasaIncrementoAnualPct / 100m), i);
            decimal ing = Math.Round(ingresosBase * factor, 2);
            decimal gas = Math.Round(gastosBase * factor, 2);
            proyeccion.Add(new ProyeccionAnualDto(anioBase + i, ing, gas, ing - gas));
        }

        return proyeccion;
    }
}