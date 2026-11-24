using ClosedXML.Excel;
using SisPH.Negocio.Interfaces;

namespace SisPH.Negocio.Servicios;

public class ExportadorExcel : IExportadorExcel
{
    public byte[] ExportarTabla<T>(
        string nombreHoja,
        string tituloReporte,
        IEnumerable<T> filas,
        Dictionary<string, Func<T, object?>> columnas)
    {
        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add(nombreHoja.Length > 31 ? nombreHoja[..31] : nombreHoja);

        // Encabezado del documento
        hoja.Cell(1, 1).Value = "SisPH - Sistema de Administración de Propiedad Horizontal";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 13;
        hoja.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A5F");

        hoja.Cell(2, 1).Value = tituloReporte;
        hoja.Cell(2, 1).Style.Font.Bold = true;
        hoja.Cell(2, 1).Style.Font.FontSize = 11;

        hoja.Cell(3, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
        hoja.Cell(3, 1).Style.Font.Italic = true;
        hoja.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;

        // Cabeceras de la tabla (fila 5)
        int filaCabecera = 5;
        int col = 1;
        foreach (var encabezado in columnas.Keys)
        {
            var celda = hoja.Cell(filaCabecera, col);
            celda.Value = encabezado;
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            col++;
        }

        // Datos
        int filaActual = filaCabecera + 1;
        foreach (var item in filas)
        {
            col = 1;
            foreach (var selector in columnas.Values)
            {
                var valor = selector(item);
                var celda = hoja.Cell(filaActual, col);

                if (valor is DateTime dt)
                {
                    celda.Value = dt;
                    celda.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }
                else if (valor is decimal dec)
                {
                    celda.Value = dec;
                    celda.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (valor is int entero)
                {
                    celda.Value = entero;
                }
                else
                {
                    celda.Value = valor?.ToString() ?? string.Empty;
                }

                col++;
            }
            filaActual++;
        }

        if (filaActual > filaCabecera + 1)
        {
            var rangoTabla = hoja.Range(filaCabecera, 1, filaActual - 1, columnas.Count);
            rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        }

        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}