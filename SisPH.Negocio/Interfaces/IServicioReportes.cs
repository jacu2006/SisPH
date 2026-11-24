using SisPH.Entidades.DTOs;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public record IngresoMensualDto(string Periodo, int CantidadPagos, decimal TotalRecaudado);
public record UsoAreaSocialDto(string AreaSocial, int TotalReservas, int AprobadasOUsadas, decimal IngresosEstimados);
public record ProyeccionAnualDto(int Anio, decimal IngresosProyectados, decimal GastosProyectados, decimal BalanceNeto);

public interface IServicioReportes
{
    Task<List<MorosidadView>> ObtenerMorosidadAsync(int? torreId = null, bool soloDosOMasMeses = false);
    Task<List<IngresoMensualDto>> ObtenerIngresosPorMesAsync(DateTime? desde = null, DateTime? hasta = null);
    Task<List<UsoAreaSocialDto>> ObtenerUsoAreasSocialesAsync(int? areaId = null, DateTime? desde = null, DateTime? hasta = null);
    Task<byte[]> ExportarMorosidadExcelAsync(int? torreId, bool soloDosOMasMeses, int adminId);
    Task<byte[]> ExportarIngresosExcelAsync(DateTime? desde, DateTime? hasta, int adminId);
    Task<byte[]> ExportarUsoAreasExcelAsync(int? areaId, DateTime? desde, DateTime? hasta, int adminId);

    Task<List<Presupuesto>> ListarPresupuestoPorAnioAsync(int anio);
    Task<ResultadoOperacion> GuardarLineaPresupuestoAsync(int anio, string concepto, string tipo, decimal montoProyectado, int adminId);
    Task<List<ProyeccionAnualDto>> CalcularProyeccionQuinquenalAsync(int anioBase, decimal tasaIncrementoAnualPct = 3.0m);
}