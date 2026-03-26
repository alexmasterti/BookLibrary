using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;

namespace BookLibrary.Services.Authors;

/// <summary>
/// Business logic layer for all author operations.
/// Implements IAuthorService to satisfy the Dependency Inversion Principle.
///
/// PATTERN: Service Layer
///   Sits between the API controllers and data access (repositories).
///   Controllers never touch the database. Repositories never contain
///   business rules. All application logic lives here.
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   This class depends on IAuthorRepository (abstraction), not
///   AuthorRepository (concrete). It also implements IAuthorService so
///   controllers depend on an abstraction — never on this class directly.
///
/// PRINCIPLE: Single Responsibility (SOLID — 'S')
///   This class orchestrates author business operations and delegates
///   persistence to the repository.
/// </summary>
public class AuthorService : IAuthorService
{
    // CONCEPT: Dependency Injection
    //   The repository is injected by the DI container (Program.cs).
    //   This class never calls 'new AuthorRepository()'.
    private readonly IAuthorRepository _repository;

    public AuthorService(IAuthorRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<List<Author>> GetAllAuthorsAsync()
        => await _repository.GetAllAsync();

    /// <inheritdoc/>
    public async Task<Author?> GetAuthorByIdAsync(int id)
        => await _repository.GetByIdAsync(id);

    /// <inheritdoc/>
    public Task AddAuthorAsync(Author author)
        => _repository.AddAsync(author);

    /// <inheritdoc/>
    public Task UpdateAuthorAsync(Author author)
        => _repository.UpdateAsync(author);

    /// <inheritdoc/>
    public Task DeleteAuthorAsync(int id)
        => _repository.DeleteAsync(id);
}
