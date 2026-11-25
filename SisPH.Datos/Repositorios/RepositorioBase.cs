using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SisPH.Datos.Contexto;
using SisPH.Datos.Interfaces;

namespace SisPH.Datos.Repositorios;

public class RepositorioBase<T> : IRepositorio<T> where T : class
{
    protected readonly SisPHDbContext _contexto;
    protected readonly DbSet<T> _dbSet;

    public RepositorioBase(SisPHDbContext contexto)
    {
        _contexto = contexto;
        _dbSet = contexto.Set<T>();
    }

    public IQueryable<T> Consultar() => _dbSet.AsQueryable();

    public async Task<T?> ObtenerPorIdAsync(object id) => await _dbSet.FindAsync(id);

    public async Task<T?> ObtenerPrimeroAsync(Expression<Func<T, bool>> predicado) =>
        await _dbSet.FirstOrDefaultAsync(predicado);

    public async Task<List<T>> ListarAsync() => await _dbSet.ToListAsync();

    public async Task<List<T>> BuscarAsync(Expression<Func<T, bool>> predicado) =>
        await _dbSet.Where(predicado).ToListAsync();

    public async Task<bool> ExisteAsync(Expression<Func<T, bool>> predicado) =>
        await _dbSet.AnyAsync(predicado);

    public async Task AgregarAsync(T entidad)
    {
        await _dbSet.AddAsync(entidad);
        await _contexto.SaveChangesAsync();
    }

    public async Task AgregarVariasAsync(IEnumerable<T> entidades)
    {
        await _dbSet.AddRangeAsync(entidades);
        await _contexto.SaveChangesAsync();
    }

    public async Task ModificarAsync(T entidad)
    {
        var entry = _contexto.Entry(entidad);
        if (entry.State == EntityState.Detached)
        {
            _dbSet.Attach(entidad);
            entry.State = EntityState.Modified;
        }
        else if (entry.State == EntityState.Unchanged)
        {
            entry.State = EntityState.Modified;
        }

        await _contexto.SaveChangesAsync();
    }
}