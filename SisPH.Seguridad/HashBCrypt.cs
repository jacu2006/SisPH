namespace SisPH.Seguridad;

public class HashBCrypt : ITransformacionSegura
{
    private const int FactorCosto = 12;

    public string Transformar(string dato) =>
        BCrypt.Net.BCrypt.HashPassword(dato, FactorCosto);

    public bool Verificar(string dato, string hash)
    {
        if (string.IsNullOrWhiteSpace(dato) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(dato, hash);
        }
        catch
        {
            return false;
        }
    }
}