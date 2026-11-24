namespace SisPH.Seguridad;

public interface ITransformacionSegura
{
    string Transformar(string dato);
    bool Verificar(string dato, string valor);
}