using SisPH.Entidades.DTOs;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;

namespace SisPH.Web.Models;

public class CuotasGenerarViewModel
{
    public string Periodo { get; set; } = DateTime.Today.ToString("yyyy-MM");
    public TipoCuota Tipo { get; set; } = TipoCuota.Ordinaria;
    public decimal Presupuesto { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public List<VistaPreviaCuotaItem>? VistaPrevia { get; set; }
}

public class RegistrarPagoViewModel
{
    public List<Unidad> Unidades { get; set; } = new();
    public int? UnidadId { get; set; }
    public List<Cuota> CuotasPendientes { get; set; } = new();
    public List<int> CuotaIdsSeleccionadas { get; set; } = new();
    public decimal MontoPagado { get; set; }
    public MetodoPago Metodo { get; set; } = MetodoPago.ACH;
    public string? Referencia { get; set; }
}

public class SolicitarReservaViewModel
{
    public List<AreaSocial> Areas { get; set; } = new();
    public int? AreaId { get; set; }
    public AreaSocial? AreaSeleccionada { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today.AddDays(1);
    public TimeSpan HoraInicio { get; set; } = new TimeSpan(10, 0, 0);
    public TimeSpan HoraFin { get; set; } = new TimeSpan(14, 0, 0);
    public List<(TimeSpan Inicio, TimeSpan Fin)> FranjasOcupadas { get; set; } = new();
}

public class GaritaViewModel
{
    public DateTime FechaConsulta { get; set; } = DateTime.Today;
    public List<PaseVisita> Historial { get; set; } = new();
    public bool HayResultadoEscaneo { get; set; }
    public bool AccesoPermitido { get; set; }
    public string? TipoMovimiento { get; set; }
    public string? MensajeEscaneo { get; set; }
    public PaseVisita? PaseEscaneado { get; set; }
}

public class RegistrarMudanzaViewModel
{
    public List<Unidad> Unidades { get; set; } = new();
    public List<Residente> Residentes { get; set; } = new();
    public int UnidadId { get; set; }
    public TipoMudanza Tipo { get; set; } = TipoMudanza.Entrada;
    public int? ResidenteIdEntrada { get; set; }
    public TipoOcupacion TipoOcupacionEntrada { get; set; } = TipoOcupacion.Propietario;
    public string? Observacion { get; set; }
}

public class ReportesIndexViewModel
{
    public string TabActiva { get; set; } = "morosidad";
    public List<Torre> Torres { get; set; } = new();
    public List<AreaSocial> Areas { get; set; } = new();

    // Morosidad
    public int? TorreId { get; set; }
    public bool SoloDosOMasMeses { get; set; }
    public List<MorosidadView> Morosidad { get; set; } = new();

    // Ingresos
    public DateTime? DesdeIngresos { get; set; }
    public DateTime? HastaIngresos { get; set; }
    public List<IngresoMensualDto> Ingresos { get; set; } = new();

    // Uso de áreas
    public int? AreaId { get; set; }
    public DateTime? DesdeAreas { get; set; }
    public DateTime? HastaAreas { get; set; }
    public List<UsoAreaSocialDto> UsoAreas { get; set; } = new();
}

public class PresupuestoViewModel
{
    public int AnioBase { get; set; } = DateTime.Today.Year;
    public decimal TasaIncrementoPct { get; set; } = 3.0m;
    public List<Presupuesto> Lineas { get; set; } = new();
    public List<ProyeccionAnualDto> ProyeccionQuinquenal { get; set; } = new();
}

public class AuditoriaViewModel
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Accion { get; set; }
    public string? Origen { get; set; }
    public List<RegistroBitacoraAuditoria> Bitacora { get; set; } = new();
    public List<LogError> Errores { get; set; } = new();
}

public class PanelViewModel
{
    // Indicadores Administrador
    public int UnidadesActivas { get; set; }
    public int UnidadesMorosas { get; set; }
    public decimal TotalMorosidad { get; set; }
    public int ReservasPendientes { get; set; }

    // Indicadores Operador (y compartidos con Admin)
    public int AccesosHoy { get; set; }
    public int VisitantesEnSitio { get; set; }
    public int IncidenciasAbiertas { get; set; }

    // Indicadores Residente
    public EstadoCuentaResidenteDto? PortalResidente { get; set; }
}
