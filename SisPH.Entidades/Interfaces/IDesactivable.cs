namespace SisPH.Entidades.Interfaces;

public interface IDesactivable
{
    bool Activo { get; set; }
    void DarDeBaja();
}