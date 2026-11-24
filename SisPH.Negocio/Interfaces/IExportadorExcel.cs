namespace SisPH.Negocio.Interfaces;

public interface IExportadorExcel
{
    byte[] ExportarTabla<T>(string nombreHoja, string tituloReporte, IEnumerable<T> filas, Dictionary<string, Func<T, object?>> columnas);
}