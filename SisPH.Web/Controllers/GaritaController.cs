using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
public class GaritaController : Controller
{
    private readonly IServicioPases _pases;

    public GaritaController(IServicioPases pases)
    {
        _pases = pases;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? fecha = null)
    {
        var dia = (fecha ?? DateTime.Today).Date;
        var modelo = new GaritaViewModel
        {
            FechaConsulta = dia,
            Historial = await _pases.ListarHistorialGaritaAsync(dia)
        };
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Escanear(string tokenQr)
    {
        var res = await _pases.RegistrarEscaneoGaritaAsync(tokenQr, User.ObtenerUsuarioId());

        var modelo = new GaritaViewModel
        {
            FechaConsulta = DateTime.Today,
            Historial = await _pases.ListarHistorialGaritaAsync(DateTime.Today),
            HayResultadoEscaneo = true,
            AccesoPermitido = res.Exito,
            MensajeEscaneo = res.Mensaje,
            PaseEscaneado = res.Datos,
            TipoMovimiento = res.Exito && res.Datos is not null
                ? (res.Datos.Salida.HasValue ? "Salida" : "Entrada")
                : null
        };

        return View(nameof(Index), modelo);
    }
}
