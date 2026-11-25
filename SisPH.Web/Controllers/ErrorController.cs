using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SisPH.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult Index(long refId = 0)
    {
        ViewBag.RefId = refId;
        return View();
    }

    [Route("Error/{statusCode:int}")]
    public IActionResult CodigoEstado(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        return View();
    }
}
