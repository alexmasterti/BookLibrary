using BookLibrary.Data;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories;

/// <summary>
/// Generic EF Core repository that implements IRepository&lt;T&gt;
/// for any entity that inherits BaseEntity.
///
/// PATTERN: Generic Repository (base implementation)
///   All five CRUD operations are implemented once here using
///   DbContext.Set&lt;T&gt;(), which returns the correct DbSet
///   for any entity type at runtime. Concrete repositories (e.g.,
///   BookRepository) inherit this class and override only what they
///   need to specialise.
///
/// CONCEPT: Inheritance (OOP Pillar)
///   BookRepository extends Repository&lt;Book&gt;. It inherits all five
///   operations and overrides only GetAllAsync to add title ordering.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   GetAllAsync is marked virtual so subclasses can override it.
///   The caller always calls the interface method; .NET dispatches
///   to the most-derived implementation at runtime.
///
/// CONCEPT: Encapsulation (OOP Pillar)
///   _context is protected (subclasses need it for typed DbSets).
///   _dbSet is private (no subclass should bypass the abstraction).
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// EF Core context. Protected so subclasses can access typed DbSets
    /// (e.g., _context.Books.OrderBy(...)) when the generic DbSet
    /// does not expose the required query capability.
    /// </summary>
    protected readonly AppDbContext _context;

    // Generic DbSet resolved at construction from the context.
    private readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc/>
    /// Virtual — subclasses override this to add ordering or eager loading.
    public virtual async Task<List<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    /// <inheritdoc/>
    public async Task AddAsync(T entity)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is not null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
