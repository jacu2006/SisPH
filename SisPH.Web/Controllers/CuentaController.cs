using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisPH.Negocio.Interfaces;
using SisPH.Web.Extensions;
using SisPH.Web.Models;

namespace SisPH.Web.Controllers;

public class CuentaController : Controller
{
    private readonly IServicioAutenticacion _auth;

    public CuentaController(IServicioAutenticacion auth)
    {
        _auth = auth;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Panel", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var resultado = await _auth.LoginAsync(model.Correo, model.Contrasena);
        if (!resultado.Exito || resultado.Datos is null)
        {
            ViewBag.ErrorLogin = resultado.Mensaje;
            return View(model);
        }

        var usuario = resultado.Datos;
        var rolNombre = usuario.Rol?.Nombre ?? "Residente";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, rolNombre),
            new("RolId", usuario.RolId.ToString())
        };

        if (usuario.Residente is not null)
            claims.Add(new Claim("ResidenteId", usuario.Residente.Id.ToString()));

        if (usuario.DebeCambiarPassword)
            claims.Add(new Claim("DebeCambiarPassword", "true"));

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identidad);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        if (usuario.DebeCambiarPassword)
        {
            TempData["MensajeError"] = "Tu cuenta utiliza una contraseña temporal. Debes actualizarla antes de operar en el sistema.";
            return RedirectToAction(nameof(CambiarPassword));
        }

        return RedirectToAction("Panel", "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public IActionResult CambiarPassword() => View(new CambiarPasswordViewModel());

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        int usuarioId = User.ObtenerUsuarioId();
        var res = await _auth.CambiarPasswordAsync(usuarioId, model.ClaveActual, model.ClaveNueva);

        if (!res.Exito)
        {
            TempData["MensajeError"] = res.Mensaje;
            return View(model);
        }

        // [#14] Refrescar la cookie retirando el claim de cambio obligatorio
        if (User.HasClaim("DebeCambiarPassword", "true"))
        {
            var nuevosClaims = User.Claims
                .Where(c => c.Type != "DebeCambiarPassword")
                .ToList();

            var identidad = new ClaimsIdentity(nuevosClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad),
                new AuthenticationProperties { IsPersistent = false });
        }

        TempData["MensajeExito"] = res.Mensaje;
        return RedirectToAction("Panel", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
