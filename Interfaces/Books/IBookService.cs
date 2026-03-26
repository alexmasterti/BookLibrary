using BookLibrary.Models;

namespace BookLibrary.Interfaces.Books;

/// <summary>
/// Defines all business operations for the book library.
///
/// PATTERN: Service Layer
///   Separates business logic from the UI (Blazor pages) and from data
///   access (repositories). The UI never talks to the database directly.
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   Blazor pages depend on this interface, not on the concrete
///   <c>BookService</c> class. This means:
///   - You can swap the real service for a mock in tests without
///     changing a single line in the UI.
///   - The concrete class can be replaced entirely without the UI
///     knowing anything changed.
///
/// CONCEPT: Abstraction (OOP Pillar)
///   The interface exposes WHAT the service can do, not HOW it does it.
/// </summary>
public interface IBookService
{
    /// <summary>Returns all books, ordered by the default strategy.</summary>
    Task<List<Book>> GetAllBooksAsync();

    /// <summary>Returns a single book by its Id, or null if not found.</summary>
    Task<Book?> GetBookByIdAsync(int id);

    /// <summary>
    /// Searches books using the Specification and Strategy patterns.
    /// </summary>
    /// <param name="query">Text to match against Title or Author (optional).</param>
    /// <param name="status">Exact status to filter by (optional).</param>
    /// <param name="sortStrategyName">
    ///   Name of the <see cref="ISortStrategy{T}"/> to apply.
    ///   Falls back to the default (first registered) strategy if null or unrecognised.
    /// </param>
    Task<List<Book>> SearchAsync(string query, ReadingStatus? status, string? sortStrategyName = null);

    /// <summary>Persists a new book to the data store.</summary>
    Task AddBookAsync(Book book);

    /// <summary>Updates an existing book in the data store.</summary>
    Task UpdateBookAsync(Book book);

    /// <summary>Removes a book by its Id.</summary>
    Task DeleteBookAsync(int id);

    /// <summary>Returns a paginated, filtered, sorted page of books.</summary>
    Task<DTOs.Common.PaginatedResult<Book>> GetPagedAsync(DTOs.Book.PagedBooksRequest request);
}
