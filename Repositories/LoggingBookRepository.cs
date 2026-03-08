using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.Extensions.Logging;

namespace BookLibrary.Repositories;

/// <summary>
/// A decorator that wraps any <see cref="IBookRepository"/> and adds
/// structured logging to every data operation.
///
/// PATTERN: Decorator
///   The Decorator pattern attaches new behaviour to an object by
///   wrapping it in another object that implements the same interface.
///
///   How it works here:
///   1. This class implements IBookRepository — same contract as BookRepository.
///   2. It stores the real repository as _inner.
///   3. Every method logs, then delegates to _inner, then optionally logs again.
///   4. BookService never knows logging is happening; it just uses IBookRepository.
///
///   Contrast with Inheritance:
///   You could subclass BookRepository and override every method.
///   But the Decorator is more flexible:
///   - It wraps ANY IBookRepository (including future ones or mocks).
///   - It can be stacked: LoggingRepo(CachingRepo(BookRepository)).
///   - BookRepository stays focused on data access; this stays focused on logging.
///
/// PRINCIPLE: Open/Closed (SOLID — 'O')
///   BookRepository is closed for modification, but we extended its
///   behaviour by decorating it — without touching its source code.
///
/// PRINCIPLE: Single Responsibility (SOLID — 'S')
///   BookRepository is responsible for data access.
///   LoggingBookRepository is responsible for logging.
///   Each class does one thing.
///
/// HOW IT IS WIRED:
///   In Program.cs, BookRepository is registered as its concrete type.
///   IBookRepository is then registered via a factory that wraps
///   BookRepository inside this decorator. The rest of the app only
///   ever sees IBookRepository — unaware of the layering.
/// </summary>
public class LoggingBookRepository : IBookRepository
{
    // The real repository that does the actual database work.
    private readonly IBookRepository _inner;
    private readonly ILogger<LoggingBookRepository> _logger;

    public LoggingBookRepository(IBookRepository inner, ILogger<LoggingBookRepository> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<Book>> GetAllAsync()
    {
        _logger.LogInformation("[Repo] GetAllAsync — fetching all books");
        var books = await _inner.GetAllAsync();
        _logger.LogInformation("[Repo] GetAllAsync — returned {Count} books", books.Count);
        return books;
    }

    /// <inheritdoc/>
    public async Task<Book?> GetByIdAsync(int id)
    {
        _logger.LogInformation("[Repo] GetByIdAsync — id={Id}", id);
        var book = await _inner.GetByIdAsync(id);
        if (book is null)
            _logger.LogWarning("[Repo] GetByIdAsync — no book found for id={Id}", id);
        return book;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Book entity)
    {
        _logger.LogInformation("[Repo] AddAsync — title='{Title}'", entity.Title);
        await _inner.AddAsync(entity);
        _logger.LogInformation("[Repo] AddAsync — saved with id={Id}", entity.Id);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Book entity)
    {
        _logger.LogInformation("[Repo] UpdateAsync — id={Id}, title='{Title}'", entity.Id, entity.Title);
        await _inner.UpdateAsync(entity);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("[Repo] DeleteAsync — id={Id}", id);
        await _inner.DeleteAsync(id);
        _logger.LogInformation("[Repo] DeleteAsync — id={Id} removed", id);
    }
}
