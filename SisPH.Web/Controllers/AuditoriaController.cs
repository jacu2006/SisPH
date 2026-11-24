using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AuditoriaController : Controller
{
    private const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IServicioBitacora _bitacora;
    private readonly IExportadorExcel _excel;

    public AuditoriaController(IServicioBitacora bitacora, IExportadorExcel excel)
    {
        _bitacora = bitacora;
        _excel = excel;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? desde = null,
        DateTime? hasta = null,
        string? accion = null,
        string? origen = null)
    {
        var modelo = new AuditoriaViewModel
        {
            Desde = desde,
            Hasta = hasta,
            Accion = accion,
            Origen = origen,
            Bitacora = await _bitacora.ConsultarBitacoraAsync(desde, hasta, null, accion),
            Errores = await _bitacora.ConsultarErroresAsync(desde, hasta, origen)
        };

        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> ExportarBitacora(
        DateTime? desde = null,
        DateTime? hasta = null,
        string? accion = null)
    {
        var filas = await _bitacora.ConsultarBitacoraAsync(desde, hasta, null, accion);

        var bytes = _excel.ExportarTabla(
            "BitacoraAuditoria",
            "Auditoría Criptográfica de Bitácora (HMAC-SHA256)",
            filas,
            new Dictionary<string, Func<RegistroBitacoraAuditoria, object?>>
            {
                ["ID"] = x => x.Registro.Id.ToString(),
                ["Fecha"] = x => x.Registro.Fecha,
                ["Usuario"] = x => x.Registro.Usuario?.Nombre ?? "Sistema",
                ["Acción"] = x => x.Registro.Accion,
                ["Entidad"] = x => x.Registro.Entidad,
                ["ID Entidad"] = x => x.Registro.EntidadId ?? string.Empty,
                ["Detalle"] = x => x.Registro.Detalle ?? string.Empty,
                ["Integridad HMAC"] = x => x.FirmaValida ? "Firma Válida" : "Alterada"
            });

        await _bitacora.RegistrarAsync(
            User.ObtenerUsuarioId(),
            "ExportarExcel",
            "Bitacora",
            null,
            $"Exportados {filas.Count} registros de bitácora a Excel.");

        return File(bytes, ExcelMime, $"SisPH_Bitacora_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}
