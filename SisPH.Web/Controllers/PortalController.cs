using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;

namespace SisPH.Web.Controllers;

[Authorize(Roles = "Residente")]
public class PortalController : Controller
{
    private readonly IServicioCuenta _cuenta;

    public PortalController(IServicioCuenta cuenta)
    {
        _cuenta = cuenta;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var portal = await _cuenta.ObtenerPortalResidenteAsync(User.ObtenerUsuarioId());
        return View(portal);
    }
}
