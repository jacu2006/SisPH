namespace SisPH.Negocio.Modelos;

public class ResultadoOperacion
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    public static ResultadoOperacion Ok(string mensaje = "Operación realizada correctamente.") =>
        new() { Exito = true, Mensaje = mensaje };

    public static ResultadoOperacion Error(string mensaje) =>
        new() { Exito = false, Mensaje = mensaje };
}

public class ResultadoOperacion<T> : ResultadoOperacion
{
    public T? Datos { get; set; }

    public static ResultadoOperacion<T> Ok(T datos, string mensaje = "Operación realizada correctamente.") =>
        new() { Exito = true, Mensaje = mensaje, Datos = datos };

    public new static ResultadoOperacion<T> Error(string mensaje) =>
        new() { Exito = false, Mensaje = mensaje };
}