using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Datos.Repositorios;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Servicios;
using SisPH.Seguridad;
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

// 6. Autenticación por cookies seguras (OWASP A07)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.LogoutPath = "/Cuenta/Logout";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SisPH.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// 7. MVC con validación global de tokens Antiforgery en peticiones POST (RNF-01 / OWASP A05)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Generar el hash BCrypt real para admin@sisph.com si aún conserva el hash inicial de plantilla
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SisPHDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredKeyedService<ITransformacionSegura>("hash");

    var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == "admin@sisph.com");
    if (admin is not null && admin.PasswordHash.StartsWith("$2a$12$R9h/cIPz0gi."))
    {
        admin.PasswordHash = hasher.Transformar("Admin123*");
        admin.IntentosFallidos = 0;
        admin.BloqueadoHasta = null;
        await db.SaveChangesAsync();
    }
}

// 8. Middleware global de excepciones (RNF-06)
app.UseMiddleware<ManejoErroresMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();