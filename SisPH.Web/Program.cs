using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Datos.Repositorios;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Servicios;
using SisPH.Seguridad;
using SisPH.Web.Filters;
using SisPH.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Serilog con archivo diario en carpeta logs/ (RNF-06)
var rutaLogs = Path.Combine(builder.Environment.ContentRootPath, "..", "logs", "sisph-.log");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(rutaLogs, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Base de datos SQL Server 2022
var connectionString = builder.Configuration.GetConnectionString("SisPHDb")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'SisPHDb'.");

builder.Services.AddDbContext<SisPHDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Repositorios genéricos y de errores (Capa de Datos)
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(RepositorioBase<>));
builder.Services.AddScoped<IRepositorioLogError, RepositorioLogError>();

// 4. Contrato criptográfico con inyección por clave (RF-03 / SOLID)
builder.Services.AddKeyedScoped<ITransformacionSegura, HashBCrypt>("hash");
builder.Services.AddKeyedScoped<ITransformacionSegura, FirmaHmacSha256>("firma");

// 5. Servicios de negocio y transversales (Capa de Negocio)
builder.Services.AddScoped<IServicioBitacora, ServicioBitacora>();
builder.Services.AddScoped<IServicioQR, ServicioQR>();
builder.Services.AddScoped<IServicioArchivos, ServicioArchivos>();
builder.Services.AddScoped<IExportadorExcel, ExportadorExcel>();
builder.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
builder.Services.AddScoped<IServicioUsuarios, ServicioUsuarios>();
builder.Services.AddScoped<IServicioUnidades, ServicioUnidades>();
builder.Services.AddScoped<IServicioResidentes, ServicioResidentes>();
builder.Services.AddScoped<IServicioAreas, ServicioAreas>();
builder.Services.AddScoped<IServicioCuotas, ServicioCuotas>();
builder.Services.AddScoped<IServicioPagos, ServicioPagos>();
builder.Services.AddScoped<IServicioReservas, ServicioReservas>();
builder.Services.AddScoped<IServicioIncidencias, ServicioIncidencias>();
builder.Services.AddScoped<IServicioPases, ServicioPases>();
builder.Services.AddScoped<IServicioMudanzas, ServicioMudanzas>();
builder.Services.AddScoped<IServicioCuenta, ServicioCuenta>();
builder.Services.AddScoped<IServicioReportes, ServicioReportes>();
builder.Services.AddScoped<ForzarCambioPasswordFilter>();

// 6. Autenticación por cookies estrictas con revalidación en BD (#5 y #12 / OWASP A07)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.LogoutPath = "/Cuenta/Logout";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SisPH.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;

        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var idClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out int usuarioId) || usuarioId <= 0)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<SisPHDbContext>();
            var estadoUsuario = await db.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => new
                {
                    u.Activo,
                    u.RolId,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : null
                })
                .FirstOrDefaultAsync();

            var rolIdClaim = principal?.FindFirstValue("RolId");
            var rolNombreClaim = principal?.FindFirstValue(ClaimTypes.Role);

            bool rolInvalido =
                (int.TryParse(rolIdClaim, out int rolIdEnCookie) && estadoUsuario is not null && estadoUsuario.RolId != rolIdEnCookie) ||
                (estadoUsuario is not null && !string.Equals(estadoUsuario.RolNombre, rolNombreClaim, StringComparison.OrdinalIgnoreCase));

            if (estadoUsuario is null || !estadoUsuario.Activo || rolInvalido)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            bool debeCambiarPassword = principal?.HasClaim("DebeCambiarPassword", "true") == true;
            context.HttpContext.Items["DebeCambiarPassword"] = debeCambiarPassword;
        };
    });

builder.Services.AddAuthorization();

// 7. MVC con Antiforgery global, AuthorizeFilter global (#13) y cambio obligatorio de clave temporal (#14)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new AuthorizeFilter());
    options.Filters.AddService<ForzarCambioPasswordFilter>();
});

var app = builder.Build();

// [#30] Actualizar el hash inicial de admin@sisph.com solo si aún conserva el marcador de plantilla
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SisPHDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredKeyedService<ITransformacionSegura>("hash");

    var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == "admin@sisph.com");
    if (admin is not null && admin.PasswordHash.StartsWith("$2a$12$R9h/cIPz0gi.", StringComparison.Ordinal))
    {
        admin.PasswordHash = hasher.Transformar("Admin123*");
        admin.IntentosFallidos = 0;
        admin.BloqueadoHasta = null;
        await db.SaveChangesAsync();
    }
}

// [#4 y #34] Cultura determinista es-PA con punto decimal para evitar interpretaciones erróneas
var culturaDeterminista = (CultureInfo)CultureInfo.GetCultureInfo("es-PA").Clone();
culturaDeterminista.NumberFormat.NumberDecimalSeparator = ".";
culturaDeterminista.NumberFormat.CurrencyDecimalSeparator = ".";
culturaDeterminista.NumberFormat.NumberGroupSeparator = ",";
culturaDeterminista.NumberFormat.CurrencyGroupSeparator = ",";

var opcionesLocalizacion = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaDeterminista),
    SupportedCultures = new List<CultureInfo> { culturaDeterminista, CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { culturaDeterminista, CultureInfo.InvariantCulture },
    RequestCultureProviders = new List<IRequestCultureProvider>()
};

// 8. Middleware global de excepciones (RNF-06)
app.UseMiddleware<ManejoErroresMiddleware>();

// [#32] Encabezados de seguridad HTTP obligatorios
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(opcionesLocalizacion);
app.UseStaticFiles();

// [#32] Páginas amigables para códigos 404 y 403
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
