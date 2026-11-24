using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SisPH.Entidades.DTOs;
using SisPH.Entidades.Entidades;
using System.Reflection.Emit;

namespace SisPH.Datos.Contexto;

public class SisPHDbContext : DbContext
{
    public SisPHDbContext(DbContextOptions<SisPHDbContext> options) : base(options)
    {
    }

    // 18 tablas del modelo Entidad/Relación
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<LogError> LogErrores => Set<LogError>();
    public DbSet<Torre> Torres => Set<Torre>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Residente> Residentes => Set<Residente>();
    public DbSet<Ocupacion> Ocupaciones => Set<Ocupacion>();
    public DbSet<Mudanza> Mudanzas => Set<Mudanza>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PagoDetalle> PagoDetalles => Set<PagoDetalle>();
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();
    public DbSet<AreaSocial> AreasSociales => Set<AreaSocial>();
    public DbSet<ImagenArea> ImagenesArea => Set<ImagenArea>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<PaseVisita> PasesVisita => Set<PaseVisita>();

    // Vista SQL Server
    public DbSet<MorosidadView> Morosidad => Set<MorosidadView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Nombres exactos de tablas en singular (coinciden con SisPH.sql)
        modelBuilder.Entity<Rol>().ToTable("Rol");
        modelBuilder.Entity<Usuario>().ToTable("Usuario");
        modelBuilder.Entity<Bitacora>().ToTable("Bitacora", tb => tb.HasTrigger("TR_Bitacora_SoloInsertar"));
        modelBuilder.Entity<LogError>().ToTable("LogError");
        modelBuilder.Entity<Torre>().ToTable("Torre");
        modelBuilder.Entity<Unidad>().ToTable("Unidad");
        modelBuilder.Entity<Residente>().ToTable("Residente");
        modelBuilder.Entity<Ocupacion>().ToTable("Ocupacion");
        modelBuilder.Entity<Mudanza>().ToTable("Mudanza");
        modelBuilder.Entity<Cuota>().ToTable("Cuota");
        modelBuilder.Entity<Pago>().ToTable("Pago");
        modelBuilder.Entity<PagoDetalle>().ToTable("PagoDetalle");
        modelBuilder.Entity<Presupuesto>().ToTable("Presupuesto");
        modelBuilder.Entity<AreaSocial>().ToTable("AreaSocial");
        modelBuilder.Entity<ImagenArea>().ToTable("ImagenArea");
        modelBuilder.Entity<Reserva>().ToTable("Reserva");
        modelBuilder.Entity<Incidencia>().ToTable("Incidencia");
        modelBuilder.Entity<PaseVisita>().ToTable("PaseVisita");

        // Vista vw_Morosidad
        modelBuilder.Entity<MorosidadView>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_Morosidad");
            e.Property(m => m.SaldoAdeudado).HasPrecision(12, 2);
        });

        // Conversiones de Enums a NVARCHAR
        modelBuilder.Entity<Ocupacion>()
            .Property(o => o.Tipo)
            .HasConversion<string>()
            .HasMaxLength(15);

        modelBuilder.Entity<Mudanza>()
            .Property(m => m.Tipo)
            .HasConversion<string>()
            .HasMaxLength(10);

        modelBuilder.Entity<Cuota>(e =>
        {
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(15);
            e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(10);
            e.Property(c => c.Monto).HasPrecision(10, 2);
            e.Property(c => c.Recargo).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Pago>(e =>
        {
            e.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(15);
            e.Property(p => p.Monto).HasPrecision(10, 2);
        });

        modelBuilder.Entity<PagoDetalle>()
            .Property(pd => pd.MontoAplicado)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Presupuesto>()
            .Property(p => p.MontoProyectado)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Unidad>(e =>
        {
            e.Property(u => u.Metros2).HasPrecision(8, 2);
            e.Property(u => u.Coeficiente).HasPrecision(7, 4);
        });

        modelBuilder.Entity<AreaSocial>(e =>
        {
            e.Property(a => a.Costo).HasPrecision(8, 2);
            e.Property(a => a.Deposito).HasPrecision(8, 2);
        });

        modelBuilder.Entity<Reserva>()
            .Property(r => r.Estado)
            .HasConversion<string>()
            .HasMaxLength(10);

        modelBuilder.Entity<Incidencia>()
            .Property(i => i.Estado)
            .HasConversion<string>()
            .HasMaxLength(12);

        // Relaciones explícitas (evita columnas sombra en EF Core)
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Rol)
            .WithMany(r => r.Usuarios)
            .HasForeignKey(u => u.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Residente>()
            .HasOne(r => r.Usuario)
            .WithOne(u => u.Residente)
            .HasForeignKey<Residente>(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bitacora>()
            .HasOne(b => b.Usuario)
            .WithMany()
            .HasForeignKey(b => b.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LogError>()
            .HasOne(l => l.Usuario)
            .WithMany()
            .HasForeignKey(l => l.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unidad>()
            .HasOne(u => u.Torre)
            .WithMany(t => t.Unidades)
            .HasForeignKey(u => u.TorreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ocupacion>()
            .HasOne(o => o.Unidad)
            .WithMany(u => u.Ocupaciones)
            .HasForeignKey(o => o.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ocupacion>()
            .HasOne(o => o.Residente)
            .WithMany(r => r.Ocupaciones)
            .HasForeignKey(o => o.ResidenteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mudanza>()
            .HasOne(m => m.Unidad)
            .WithMany(u => u.Mudanzas)
            .HasForeignKey(m => m.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mudanza>()
            .HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cuota>()
            .HasOne(c => c.Unidad)
            .WithMany(u => u.Cuotas)
            .HasForeignKey(c => c.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Unidad)
            .WithMany(u => u.Pagos)
            .HasForeignKey(p => p.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Usuario)
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PagoDetalle>()
            .HasOne(pd => pd.Pago)
            .WithMany(p => p.Detalles)
            .HasForeignKey(pd => pd.PagoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PagoDetalle>()
            .HasOne(pd => pd.Cuota)
            .WithMany(c => c.PagoDetalles)
            .HasForeignKey(pd => pd.CuotaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImagenArea>()
            .HasOne(i => i.AreaSocial)
            .WithMany(a => a.Imagenes)
            .HasForeignKey(i => i.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.AreaSocial)
            .WithMany(a => a.Reservas)
            .HasForeignKey(r => r.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.Residente)
            .WithMany(res => res.Reservas)
            .HasForeignKey(r => r.ResidenteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.UsuarioAprobador)
            .WithMany()
            .HasForeignKey(r => r.AprobadaPor)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incidencia>()
            .HasOne(i => i.Residente)
            .WithMany(r => r.Incidencias)
            .HasForeignKey(i => i.ResidenteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaseVisita>()
            .HasOne(p => p.Residente)
            .WithMany(r => r.PasesVisita)
            .HasForeignKey(p => p.ResidenteId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    // Ejecución de procedimientos almacenados documentados (sección 8.1)
    public async Task EjecutarGenerarCuotasAsync(string periodo, decimal presupuestoMensual, string tipo = "Ordinaria", DateTime? vence = null)
    {
        var parametros = new[]
        {
            new SqlParameter("@Periodo", periodo),
            new SqlParameter("@PresupuestoMensual", presupuestoMensual),
            new SqlParameter("@Tipo", tipo),
            new SqlParameter("@Vence", (object?)vence?.Date ?? DBNull.Value)
        };

        await Database.ExecuteSqlRawAsync(
            "EXEC sp_GenerarCuotas @Periodo, @PresupuestoMensual, @Tipo, @Vence",
            parametros);
    }

    public async Task EjecutarActualizarEstadosCuotaAsync(decimal porcentajeRecargo = 10.00m, DateTime? fechaCorte = null)
    {
        var parametros = new[]
        {
            new SqlParameter("@PorcentajeRecargo", porcentajeRecargo),
            new SqlParameter("@FechaCorte", (object?)fechaCorte?.Date ?? DBNull.Value)
        };

        await Database.ExecuteSqlRawAsync(
            "EXEC sp_ActualizarEstadosCuota @PorcentajeRecargo, @FechaCorte",
            parametros);
    }
}