using BookLibrary.Models;

namespace BookLibrary.Interfaces.Common;

/// <summary>
/// Generic repository interface for standard CRUD operations.
///
/// PATTERN: Generic Repository
///   Defines data access operations once for any entity type T.
///   If you add an Author or Magazine entity later, you get all
///   five operations for free by implementing this interface.
///
/// PATTERN: Repository
///   Abstracts the data layer behind an interface. The service layer
///   (BookService) works with IRepository&lt;T&gt; and never references
///   Entity Framework, SQL, or any storage technology directly.
///   Swapping SQLite for PostgreSQL requires changing only the
///   infrastructure layer — zero changes to services or UI.
///
/// PRINCIPLE: Interface Segregation (SOLID — 'I')
///   This interface contains only the operations every repository needs.
///   Entity-specific operations go in a derived interface (IBookRepository).
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   High-level modules (services) depend on this abstraction.
///   Low-level modules (EF Core repositories) implement it.
///
/// CONSTRAINT: T must inherit BaseEntity.
///   This guarantees every entity has an integer Id, which is required
///   for GetByIdAsync and DeleteAsync.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Returns all entities of type T.</summary>
    Task<List<T>> GetAllAsync();

    /// <summary>Returns the entity with the given Id, or null if not found.</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Persists a new entity to the data store.</summary>
    Task AddAsync(T entity);

    /// <summary>Updates an existing entity in the data store.</summary>
    Task UpdateAsync(T entity);

    /// <summary>Removes the entity with the given Id from the data store.</summary>
    Task DeleteAsync(int id);
}
