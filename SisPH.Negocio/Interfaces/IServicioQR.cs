namespace SisPH.Negocio.Interfaces;

public interface IServicioQR
{
    byte[] GenerarPngBytes(string contenido);
    string GenerarPngBase64(string contenido);
}