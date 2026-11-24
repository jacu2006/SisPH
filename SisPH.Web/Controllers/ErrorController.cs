using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SisPH.Web.Controllers;

public class ErrorController : Controller
{
    [AllowAnonymous]
    [Route("Error")]
    public IActionResult Index(long refId = 0)
    {
        ViewBag.RefId = refId;
        return View();
    }
}