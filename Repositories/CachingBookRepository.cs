using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BookLibrary.Repositories;

/// <summary>
/// A decorator that adds in-memory caching to any IBookRepository.
///
/// PATTERN: Decorator (second layer)
///   This is a second Decorator wrapping LoggingBookRepository,
///   which itself wraps BookRepository. The full chain at runtime:
///
///   BookService
///     → CachingBookRepository   (this class — handles caching)
///       → LoggingBookRepository (logs every operation)
///         → BookRepository      (hits the database via EF Core)
///
///   Each layer has a single responsibility. The service has no idea
///   how many decorators exist — it only sees IBookRepository.
///
/// STRATEGY: Cache-aside (read-through / write-invalidate)
///   On reads  — serve from cache if available; otherwise fetch from
///               inner repository and populate the cache.
///   On writes — invalidate the affected cache entries so stale data
///               is never served.
///
/// CONCEPT: Options Pattern
///   Cache duration is read from IOptions&lt;CacheOptions&gt;, bound from
///   appsettings.json. No magic constants in code.
///
/// PRINCIPLE: Open/Closed (SOLID — 'O')
///   LoggingBookRepository is closed for modification. Caching was
///   added as a new wrapper, not by editing existing code.
/// </summary>
public class CachingBookRepository : IBookRepository
{
    private const string AllBooksCacheKey = "books_all";
    private static string BookCacheKey(int id) => $"book_{id}";

    private readonly IBookRepository _inner;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<CachingBookRepository> _logger;

    public CachingBookRepository(
        IBookRepository inner,
        IMemoryCache cache,
        IOptions<CacheOptions> options,
        ILogger<CachingBookRepository> logger)
    {
        _inner   = inner;
        _cache   = cache;
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc/>
    public async Task<List<Book>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllBooksCacheKey, out List<Book>? cached) && cached is not null)
        {
            _logger.LogInformation("[Cache] HIT  — books_all ({Count} books)", cached.Count);
            return cached;
        }

        _logger.LogInformation("[Cache] MISS — books_all, fetching from repository");
        var books = await _inner.GetAllAsync();

        _cache.Set(AllBooksCacheKey, books, TimeSpan.FromSeconds(_options.BooksCacheDurationSeconds));
        _logger.LogInformation("[Cache] SET  — books_all cached for {Seconds}s", _options.BooksCacheDurationSeconds);

        return books;
    }

    /// <inheritdoc/>
    public async Task<Book?> GetByIdAsync(int id)
    {
        var key = BookCacheKey(id);

        if (_cache.TryGetValue(key, out Book? cached))
        {
            _logger.LogInformation("[Cache] HIT  — {Key}", key);
            return cached;
        }

        _logger.LogInformation("[Cache] MISS — {Key}", key);
        var book = await _inner.GetByIdAsync(id);

        if (book is not null)
            _cache.Set(key, book, TimeSpan.FromSeconds(_options.BooksCacheDurationSeconds));

        return book;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Book entity)
    {
        await _inner.AddAsync(entity);
        InvalidateListCache();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Book entity)
    {
        await _inner.UpdateAsync(entity);
        InvalidateListCache();
        _cache.Remove(BookCacheKey(entity.Id));
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        await _inner.DeleteAsync(id);
        InvalidateListCache();
        _cache.Remove(BookCacheKey(id));
    }

    private void InvalidateListCache()
    {
        _cache.Remove(AllBooksCacheKey);
        _logger.LogInformation("[Cache] INVALIDATED — books_all");
    }
}
