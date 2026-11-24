using QRCoder;
using SisPH.Negocio.Interfaces;

namespace SisPH.Negocio.Servicios;

public class ServicioQR : IServicioQR
{
    public byte[] GenerarPngBytes(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ArgumentException("El contenido del código QR no puede estar vacío.", nameof(contenido));

        using var generador = new QRCodeGenerator();
        using var datosQr = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        using var pngQr = new PngByteQRCode(datosQr);
        return pngQr.GetGraphic(20);
    }

    public string GenerarPngBase64(string contenido)
    {
        var bytes = GenerarPngBytes(contenido);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}