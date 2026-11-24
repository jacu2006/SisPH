namespace SisPH.Datos.Interfaces;

public interface IRepositorioLogError
{
    Task<long> GuardarAsync(Exception ex, string origen, string? ruta, string? ip, int? usuarioId);
}