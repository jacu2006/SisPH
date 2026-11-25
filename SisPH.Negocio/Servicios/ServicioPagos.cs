using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;
using SisPH.Seguridad;

namespace SisPH.Negocio.Servicios;

public class ServicioPagos : IServicioPagos
{
    private readonly IRepositorio<Pago> _repoPago;
    private readonly IRepositorio<Cuota> _repoCuota;
    private readonly IRepositorio<PagoDetalle> _repoDetalle;
    private readonly SisPHDbContext _contexto;
    private readonly ITransformacionSegura _firma;
    private readonly IServicioQR _qr;
    private readonly IServicioBitacora _bitacora;

    public ServicioPagos(
        IRepositorio<Pago> repoPago,
        IRepositorio<Cuota> repoCuota,
        IRepositorio<PagoDetalle> repoDetalle,
        SisPHDbContext contexto,
        [FromKeyedServices("firma")] ITransformacionSegura firma,
        IServicioQR qr,
        IServicioBitacora bitacora)
    {
        _repoPago = repoPago;
        _repoCuota = repoCuota;
        _repoDetalle = repoDetalle;
        _contexto = contexto;
        _firma = firma;
        _qr = qr;
        _bitacora = bitacora;
    }

    public async Task<List<Pago>> ListarRecientesAsync(int? unidadId = null)
    {
        var query = _repoPago.Consultar()
            .Include(p => p.Unidad)
                .ThenInclude(u => u!.Torre)
            .Include(p => p.Usuario)
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Cuota)
            .AsNoTracking()
            .AsSplitQuery();

        if (unidadId.HasValue)
            query = query.Where(p => p.UnidadId == unidadId.Value);

        return await query
            .OrderByDescending(p => p.Fecha)
            .Take(200)
            .ToListAsync();
    }

    public async Task<ResultadoOperacion<ReciboVerificadoDto>> RegistrarPagoAsync(
        int unidadId,
        List<int> cuotaIds,
        decimal montoPagado,
        MetodoPago metodo,
        string? referencia,
        string urlBaseVerificacion,
        int operadorId)
    {
        if (cuotaIds is null || cuotaIds.Count == 0)
            return ResultadoOperacion<ReciboVerificadoDto>.Error("Debe seleccionar al menos una cuota para aplicar el pago.");

        if (montoPagado <= 0)
            return ResultadoOperacion<ReciboVerificadoDto>.Error("El monto del pago debe ser mayor a cero.");

        // [#3] Iniciar transacción Serializable ANTES de consultar saldos para impedir doble pago concurrente
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var cuotas = await _repoCuota.Consultar()
                .Include(c => c.PagoDetalles)
                .Where(c => c.UnidadId == unidadId && cuotaIds.Contains(c.Id) && c.Estado != EstadoCuota.Pagada)
                .OrderBy(c => c.Vence)
                .ThenBy(c => c.Id)
                .ToListAsync();

            if (cuotas.Count == 0)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<ReciboVerificadoDto>.Error("Las cuotas seleccionadas no son válidas o ya están pagadas.");
            }

            decimal deudaSeleccionada = cuotas.Sum(c =>
                Math.Max(0m, (c.Monto + c.Recargo) - c.PagoDetalles.Sum(pd => pd.MontoAplicado)));

            if (deudaSeleccionada <= 0m)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<ReciboVerificadoDto>.Error("Las cuotas seleccionadas ya no tienen saldo pendiente.");
            }

            if (montoPagado > deudaSeleccionada)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<ReciboVerificadoDto>.Error(
                    $"El monto ingresado (${montoPagado:0.00}) excede el saldo pendiente de las cuotas seleccionadas (${deudaSeleccionada:0.00}).");
            }

            var fechaPrecisa = DateTime.Now;
            decimal restante = montoPagado;
            var detallesAplicados = new List<PagoDetalle>();

            foreach (var cuota in cuotas)
            {
                if (restante <= 0) break;

                decimal yaPagado = cuota.PagoDetalles.Sum(pd => pd.MontoAplicado);
                decimal totalCuota = cuota.Monto + cuota.Recargo;
                decimal saldoCuota = totalCuota - yaPagado;

                if (saldoCuota <= 0) continue;

                decimal aplicar = Math.Min(restante, saldoCuota);
                restante -= aplicar;

                detallesAplicados.Add(new PagoDetalle
                {
                    CuotaId = cuota.Id,
                    MontoAplicado = aplicar
                });

                if (yaPagado + aplicar >= totalCuota)
                    cuota.Estado = EstadoCuota.Pagada;
            }

            if (detallesAplicados.Count == 0)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<ReciboVerificadoDto>.Error("No se pudo aplicar el pago a ninguna cuota pendiente.");
            }

            var pago = new Pago
            {
                UnidadId = unidadId,
                UsuarioId = operadorId,
                Fecha = fechaPrecisa,
                Monto = montoPagado,
                Metodo = metodo,
                Referencia = referencia?.Trim()
            };

            pago.Firma = _firma.Transformar(ConstruirPayloadRecibo(pago, detallesAplicados));

            await _repoPago.AgregarAsync(pago);

            foreach (var det in detallesAplicados)
                det.PagoId = pago.Id;

            await _repoDetalle.AgregarVariasAsync(detallesAplicados);

            foreach (var cuota in cuotas)
                await _repoCuota.ModificarAsync(cuota);

            // [#18] Incluir firma autenticada en la URL del QR
            var urlCompleta = ConstruirUrlVerificacion(urlBaseVerificacion, pago.Id, pago.Firma);
            var qrBase64 = _qr.GenerarPngBase64(urlCompleta);

            await transaccion.CommitAsync();

            await _bitacora.RegistrarAsync(
                operadorId,
                "RegistrarPago",
                "Pago",
                pago.Id.ToString(),
                $"Pago de ${montoPagado:0.00} ({metodo}) registrado para UnidadId {unidadId}.");

            var pagoCompleto = await _repoPago.Consultar()
                .Include(p => p.Unidad).ThenInclude(u => u!.Torre)
                .Include(p => p.Usuario)
                .Include(p => p.Detalles).ThenInclude(d => d.Cuota)
                .FirstAsync(p => p.Id == pago.Id);

            return ResultadoOperacion<ReciboVerificadoDto>.Ok(
                new ReciboVerificadoDto(pagoCompleto, true, qrBase64),
                "Pago registrado y recibo firmado digitalmente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ReciboVerificadoDto?> ObtenerReciboVerificadoAsync(int pagoId, string urlBaseVerificacion)
    {
        var pago = await _repoPago.Consultar()
            .Include(p => p.Unidad).ThenInclude(u => u!.Torre)
            .Include(p => p.Usuario)
            .Include(p => p.Detalles).ThenInclude(d => d.Cuota)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == pagoId);

        if (pago is null)
            return null;

        bool firmaValida = VerificarFirmaRecibo(pago);
        var qrBase64 = _qr.GenerarPngBase64(ConstruirUrlVerificacion(urlBaseVerificacion, pago.Id, pago.Firma));

        return new ReciboVerificadoDto(pago, firmaValida, qrBase64);
    }

    // [#18] Verificación pública exigiendo parámetro de firma en tiempo constante
    public async Task<ReciboVerificadoDto?> VerificarReciboPublicoAsync(int pagoId, string? firmaRecibida, string urlBaseVerificacion)
    {
        if (string.IsNullOrWhiteSpace(firmaRecibida))
            return null;

        var pago = await _repoPago.Consultar()
            .Include(p => p.Unidad).ThenInclude(u => u!.Torre)
            .Include(p => p.Usuario)
            .Include(p => p.Detalles).ThenInclude(d => d.Cuota)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == pagoId);

        if (pago is null || string.IsNullOrWhiteSpace(pago.Firma))
            return null;

        byte[] bytesAlmacenados = Encoding.UTF8.GetBytes(pago.Firma);
        byte[] bytesRecibidos = Encoding.UTF8.GetBytes(firmaRecibida.Trim());

        bool coincideToken = bytesAlmacenados.Length == bytesRecibidos.Length &&
                             CryptographicOperations.FixedTimeEquals(bytesAlmacenados, bytesRecibidos);

        if (!coincideToken)
            return null;

        bool firmaValida = VerificarFirmaRecibo(pago);
        var qrBase64 = _qr.GenerarPngBase64(ConstruirUrlVerificacion(urlBaseVerificacion, pago.Id, pago.Firma));

        return new ReciboVerificadoDto(pago, firmaValida, qrBase64);
    }

    private bool VerificarFirmaRecibo(Pago pago)
    {
        var payload = ConstruirPayloadRecibo(pago, pago.Detalles);
        if (_firma.Verificar(payload, pago.Firma))
            return true;

        if (pago.Fecha.Ticks % TimeSpan.TicksPerMillisecond == 0)
        {
            var payloadAnterior = ConstruirPayloadReciboAnterior(pago, pago.Detalles);
            return _firma.Verificar(payloadAnterior, pago.Firma);
        }

        return false;
    }

    private static string ConstruirUrlVerificacion(string urlBaseVerificacion, int pagoId, string firma) =>
        $"{urlBaseVerificacion.TrimEnd('/')}/{pagoId}?f={Uri.EscapeDataString(firma)}";

    private static string ConstruirPayloadRecibo(Pago pago, IEnumerable<PagoDetalle> detalles)
    {
        var cuotasOrdenadas = string.Join(",",
            detalles.OrderBy(d => d.CuotaId)
                .Select(d => string.Create(CultureInfo.InvariantCulture, $"{d.CuotaId}:{d.MontoAplicado:0.00}")));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"RECIBO|{pago.UnidadId}|{pago.Monto:0.00}|{pago.Metodo}|{pago.Fecha:yyyy-MM-dd HH:mm:ss.fffffff}|{cuotasOrdenadas}");
    }

    private static string ConstruirPayloadReciboAnterior(Pago pago, IEnumerable<PagoDetalle> detalles)
    {
        var cuotasOrdenadas = string.Join(",",
            detalles.OrderBy(d => d.CuotaId)
                .Select(d => string.Create(CultureInfo.InvariantCulture, $"{d.CuotaId}:{d.MontoAplicado:0.00}")));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"RECIBO|{pago.UnidadId}|{pago.Monto:0.00}|{pago.Metodo}|{pago.Fecha:yyyy-MM-ddTHH:mm:ss.fff}|{cuotasOrdenadas}");
    }
}
