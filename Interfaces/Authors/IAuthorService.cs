using BookLibrary.Models;

namespace BookLibrary.Interfaces.Authors;

/// <summary>
/// Defines all business operations for authors.
///
/// PATTERN: Service Layer
///   Controllers and Blazor pages depend on this interface, never on
///   the concrete AuthorService. This enforces the Dependency Inversion
///   Principle and makes the service easily mockable in tests.
///
/// CONCEPT: Abstraction (OOP Pillar)
///   Exposes WHAT the service can do — not HOW it does it.
/// </summary>
public interface IAuthorService
{
    /// <summary>Returns all authors, ordered alphabetically by name.</summary>
    Task<List<Author>> GetAllAuthorsAsync();

    /// <summary>Returns a single author by Id, or null if not found.</summary>
    Task<Author?> GetAuthorByIdAsync(int id);

    /// <summary>Persists a new author to the data store.</summary>
    Task AddAuthorAsync(Author author);

    /// <summary>Updates an existing author in the data store.</summary>
    Task UpdateAuthorAsync(Author author);

    /// <summary>Removes an author by Id.</summary>
    Task DeleteAuthorAsync(int id);
}
