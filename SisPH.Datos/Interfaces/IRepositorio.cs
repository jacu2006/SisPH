using System.Linq.Expressions;

namespace SisPH.Datos.Interfaces;

public interface IRepositorio<T> where T : class
{
    IQueryable<T> Consultar();
    Task<T?> ObtenerPorIdAsync(object id);
    Task<T?> ObtenerPrimeroAsync(Expression<Func<T, bool>> predicado);
    Task<List<T>> ListarAsync();
    Task<List<T>> BuscarAsync(Expression<Func<T, bool>> predicado);
    Task<bool> ExisteAsync(Expression<Func<T, bool>> predicado);
    Task AgregarAsync(T entidad);
    Task AgregarVariasAsync(IEnumerable<T> entidades);
    Task ModificarAsync(T entidad);
}