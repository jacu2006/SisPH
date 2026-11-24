using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Interfaces;
using SisPH.Entidades.Entidades;
using SisPH.Negocio.Interfaces;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Servicios;

public class ServicioPases : IServicioPases
{
    private readonly IRepositorio<PaseVisita> _repoPase;
    private readonly IServicioQR _qr;
    private readonly IServicioBitacora _bitacora;

    public ServicioPases(
        IRepositorio<PaseVisita> repoPase,
        IServicioQR qr,
        IServicioBitacora bitacora)
    {
        _repoPase = repoPase;
        _qr = qr;
        _bitacora = bitacora;
    }

    public async Task<List<PaseVisita>> ListarPorResidenteAsync(int residenteId) =>
        await _repoPase.Consultar()
            .Where(p => p.ResidenteId == residenteId)
            .OrderByDescending(p => p.VigenteHasta)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<PaseVisita>> ListarHistorialGaritaAsync(DateTime? fecha = null)
    {
        var dia = (fecha ?? DateTime.Today).Date;
        return await _repoPase.Consultar()
            .Include(p => p.Residente)
                .ThenInclude(r => r!.Ocupaciones.Where(o => o.FechaFin == null))
                    .ThenInclude(o => o.Unidad)
                        .ThenInclude(u => u!.Torre)
            .Where(p => p.VigenteHasta >= dia || (p.Entrada.HasValue && p.Entrada.Value.Date == dia))
            .OrderByDescending(p => p.Entrada ?? p.VigenteHasta)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ResultadoOperacion<PaseGeneradoDto>> GenerarPaseAsync(
        int residenteId,
        string nombreVisitante,
        string? cedulaVisitante,
        DateTime vigenteHasta,
        int usuarioId)
    {
        if (string.IsNullOrWhiteSpace(nombreVisitante))
            return ResultadoOperacion<PaseGeneradoDto>.Error("Debe indicar el nombre del visitante.");

        var ahora = DateTime.Now;
        if (vigenteHasta <= ahora)
            return ResultadoOperacion<PaseGeneradoDto>.Error("La vigencia del pase debe ser una fecha y hora futura.");

        if (vigenteHasta > ahora.AddHours(24).AddMinutes(5))
            return ResultadoOperacion<PaseGeneradoDto>.Error("La vigencia máxima permitida para un pase de visita es de 24 horas.");

        var token = Guid.NewGuid().ToString("N"); // 32 caracteres exactos (CHAR(32))

        var pase = new PaseVisita
        {
            ResidenteId = residenteId,
            NombreVisitante = nombreVisitante.Trim(),
            CedulaVisitante = cedulaVisitante?.Trim(),
            TokenQR = token,
            VigenteHasta = vigenteHasta
        };

        await _repoPase.AgregarAsync(pase);

        var qrBase64 = _qr.GenerarPngBase64(token);

        await _bitacora.RegistrarAsync(
            usuarioId,
            "GenerarPaseQR",
            "PaseVisita",
            pase.Id.ToString(),
            $"Pase QR generado para {pase.NombreVisitante} vigente hasta {vigenteHasta:dd/MM/yyyy HH:mm}.");

        return ResultadoOperacion<PaseGeneradoDto>.Ok(
            new PaseGeneradoDto(pase, qrBase64),
            "Pase de visita generado correctamente.");
    }

    public async Task<ResultadoOperacion<PaseVisita>> RegistrarEscaneoGaritaAsync(string tokenQr, int operadorId)
    {
        if (string.IsNullOrWhiteSpace(tokenQr))
            return ResultadoOperacion<PaseVisita>.Error("Código QR vacío o inválido.");

        var tokenLimpio = tokenQr.Trim();
        var pase = await _repoPase.Consultar()
            .Include(p => p.Residente)
                .ThenInclude(r => r!.Ocupaciones.Where(o => o.FechaFin == null))
                    .ThenInclude(o => o.Unidad)
                        .ThenInclude(u => u!.Torre)
            .FirstOrDefaultAsync(p => p.TokenQR == tokenLimpio);

        if (pase is null)
        {
            await _bitacora.RegistrarAsync(
                operadorId,
                "AccesoDenegadoQR",
                "PaseVisita",
                null,
                $"Intento de acceso con token QR inexistente: {tokenLimpio}");
            return ResultadoOperacion<PaseVisita>.Error("Acceso denegado: código QR inexistente.");
        }

        if (pase.Salida.HasValue)
        {
            await _bitacora.RegistrarAsync(
                operadorId,
                "AccesoDenegadoQR",
                "PaseVisita",
                pase.Id.ToString(),
                $"Intento de reutilizar pase ya cerrado de {pase.NombreVisitante}.");
            return ResultadoOperacion<PaseVisita>.Error("Acceso denegado: este pase ya registró entrada y salida.");
        }

        if (!pase.Entrada.HasValue)
        {
            if (DateTime.Now > pase.VigenteHasta)
            {
                await _bitacora.RegistrarAsync(
                    operadorId,
                    "AccesoDenegadoQR",
                    "PaseVisita",
                    pase.Id.ToString(),
                    $"Pase vencido de {pase.NombreVisitante} (venció {pase.VigenteHasta:dd/MM/yyyy HH:mm}).");
                return ResultadoOperacion<PaseVisita>.Error("Acceso denegado: el pase QR está vencido.");
            }

            pase.MarcarEntrada();
            await _repoPase.ModificarAsync(pase);

            await _bitacora.RegistrarAsync(
                operadorId,
                "RegistrarEntradaVisitante",
                "PaseVisita",
                pase.Id.ToString(),
                $"Entrada registrada para {pase.NombreVisitante}.");

            return ResultadoOperacion<PaseVisita>.Ok(pase, $"Entrada registrada: {pase.NombreVisitante}.");
        }

        pase.MarcarSalida();
        await _repoPase.ModificarAsync(pase);

        await _bitacora.RegistrarAsync(
            operadorId,
            "RegistrarSalidaVisitante",
            "PaseVisita",
            pase.Id.ToString(),
            $"Salida registrada para {pase.NombreVisitante}.");

        return ResultadoOperacion<PaseVisita>.Ok(pase, $"Salida registrada: {pase.NombreVisitante}.");
    }
}