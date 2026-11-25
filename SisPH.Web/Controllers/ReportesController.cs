using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ReportesController : Controller
{
    private const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IServicioReportes _reportes;
    private readonly IServicioUnidades _unidades;
    private readonly IServicioAreas _areas;

    public ReportesController(
        IServicioReportes reportes,
        IServicioUnidades unidades,
        IServicioAreas areas)
    {
        _reportes = reportes;
        _unidades = unidades;
        _areas = areas;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "morosidad",
        int? torreId = null,
        bool soloDosOMasMeses = false,
        DateTime? desdeIngresos = null,
        DateTime? hastaIngresos = null,
        int? areaId = null,
        DateTime? desdeAreas = null,
        DateTime? hastaAreas = null)
    {
        var tabValida = tab is "ingresos" or "areas" ? tab : "morosidad";

        var modelo = new ReportesIndexViewModel
        {
            TabActiva = tabValida,
            Torres = await _unidades.ListarTorresAsync(),
            Areas = await _areas.ListarAsync(null),

            TorreId = torreId,
            SoloDosOMasMeses = soloDosOMasMeses,
            Morosidad = await _reportes.ObtenerMorosidadAsync(torreId, soloDosOMasMeses),

            DesdeIngresos = desdeIngresos,
            HastaIngresos = hastaIngresos,
            Ingresos = await _reportes.ObtenerIngresosPorMesAsync(desdeIngresos, hastaIngresos),

            AreaId = areaId,
            DesdeAreas = desdeAreas,
            HastaAreas = hastaAreas,
            UsoAreas = await _reportes.ObtenerUsoAreasSocialesAsync(areaId, desdeAreas, hastaAreas)
        };

        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> ExportarMorosidad(int? torreId, bool soloDosOMasMeses)
    {
        var bytes = await _reportes.ExportarMorosidadExcelAsync(torreId, soloDosOMasMeses, User.ObtenerUsuarioId());
        return File(bytes, ExcelMime, $"SisPH_Morosidad_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> ExportarIngresos(DateTime? desde, DateTime? hasta)
    {
        var bytes = await _reportes.ExportarIngresosExcelAsync(desde, hasta, User.ObtenerUsuarioId());
        return File(bytes, ExcelMime, $"SisPH_Ingresos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> ExportarUsoAreas(int? areaId, DateTime? desde, DateTime? hasta)
    {
        var bytes = await _reportes.ExportarUsoAreasExcelAsync(areaId, desde, hasta, User.ObtenerUsuarioId());
        return File(bytes, ExcelMime, $"SisPH_UsoAreas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Presupuesto(int? anio = null, decimal tasa = 3.0m)
    {
        int anioBase = anio.HasValue ? Math.Clamp(anio.Value, 2000, 2100) : DateTime.Today.Year;
        if (tasa < -50m || tasa > 100m)
        {
            TempData["MensajeError"] = "La tasa de incremento anual debe estar comprendida entre -50 % y 100 %.";
            tasa = Math.Clamp(tasa, -50m, 100m);
        }

        var modelo = new PresupuestoViewModel
        {
            AnioBase = anioBase,
            TasaIncrementoPct = tasa,
            Lineas = await _reportes.ListarPresupuestoPorAnioAsync(anioBase),
            ProyeccionQuinquenal = await _reportes.CalcularProyeccionQuinquenalAsync(anioBase, tasa)
        };

        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarPresupuesto(
        int anio,
        string concepto,
        string tipo,
        decimal montoProyectado,
        decimal tasa = 3.0m)
    {
        tasa = Math.Clamp(tasa, -50m, 100m);

        var res = await _reportes.GuardarLineaPresupuestoAsync(
            anio, concepto, tipo, montoProyectado, User.ObtenerUsuarioId());

        TempData[res.Exito ? "MensajeExito" : "MensajeError"] = res.Mensaje;
        return RedirectToAction(nameof(Presupuesto), new { anio, tasa });
    }
}
