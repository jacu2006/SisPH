using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SisPH.Seguridad;

public class FirmaHmacSha256 : ITransformacionSegura
{
    private readonly byte[] _clave;

    public FirmaHmacSha256(IConfiguration cfg)
    {
        var claveBase64 = cfg["Crypto:FirmaKey"];
        if (string.IsNullOrWhiteSpace(claveBase64))
            throw new InvalidOperationException("No se ha configurado 'Crypto:FirmaKey' en User Secrets o appsettings.");

        _clave = Convert.FromBase64String(claveBase64);
    }

    public string Transformar(string dato)
    {
        using var hmac = new HMACSHA256(_clave);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dato)));
    }

    public bool Verificar(string dato, string firma)
    {
        if (string.IsNullOrWhiteSpace(dato) || string.IsNullOrWhiteSpace(firma))
            return false;

        try
        {
            byte[] esperada = Convert.FromBase64String(Transformar(dato));
            byte[] recibida = Convert.FromBase64String(firma);

            return esperada.Length == recibida.Length &&
                   CryptographicOperations.FixedTimeEquals(esperada, recibida);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}