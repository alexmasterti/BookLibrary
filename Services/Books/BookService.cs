using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;
using BookLibrary.Specifications;

namespace BookLibrary.Services.Books;

/// <summary>
/// Business logic layer for all book library operations.
/// Implements IBookService to satisfy the Dependency Inversion Principle.
///
/// PATTERN: Service Layer
///   Sits between the UI (Blazor pages) and data access (repositories).
///   The UI never touches the database. The repository never contains
///   business rules. All application logic lives here.
///
/// PATTERNS DEMONSTRATED INSIDE THIS CLASS:
///   - Dependency Injection: receives IBookRepository and sort strategies
///     via constructor — never calls 'new' on infrastructure classes.
///   - Strategy: SearchAsync selects an ISortStrategy&lt;Book&gt; by name at runtime.
///   - Specification: filtering rules are expressed as composable
///     ISpecification&lt;Book&gt; objects rather than inline lambdas.
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   This class depends on IBookRepository (abstraction), not
///   BookRepository (concrete). It also implements IBookService so
///   the UI depends on an abstraction — never on this class directly.
///
/// PRINCIPLE: Single Responsibility (SOLID — 'S')
///   This class orchestrates business operations. It delegates
///   persistence to the repository and sorting to strategies.
/// </summary>
public class BookService : IBookService
{
    // CONCEPT: Dependency Injection
    //   Both dependencies are injected via the constructor.
    //   ASP.NET Core DI resolves them automatically based on Program.cs registrations.
    //   This class never creates its own IBookRepository or strategies —
    //   it declares what it needs and the container provides it.
    private readonly IBookRepository _repository;
    private readonly IEnumerable<ISortStrategy<Book>> _sortStrategies;

    public BookService(
        IBookRepository repository,
        IEnumerable<ISortStrategy<Book>> sortStrategies)
    {
        _repository = repository;
        _sortStrategies = sortStrategies;
    }

    /// <inheritdoc/>
    public async Task<List<Book>> GetAllBooksAsync()
        => await _repository.GetAllAsync();

    /// <inheritdoc/>
    public async Task<Book?> GetBookByIdAsync(int id)
        => await _repository.GetByIdAsync(id);

    /// <inheritdoc/>
    /// PATTERN: Specification + Strategy working together.
    ///
    /// Step 1 — Build a filter specification from the provided criteria.
    ///   Each active filter becomes an ISpecification&lt;Book&gt; object.
    ///   When both filters are active, they are combined with AndSpecification
    ///   (Composite pattern) so a single IsSatisfiedBy call covers both rules.
    ///
    /// Step 2 — Apply the selected sort strategy.
    ///   The strategy is looked up by name from the injected collection.
    ///   If no name matches, the first registered strategy is the default.
    ///   The service contains zero if/else sort logic — it delegates
    ///   entirely to the strategy via polymorphic dispatch.
    public async Task<List<Book>> SearchAsync(
        string query,
        ReadingStatus? status,
        string? sortStrategyName = null)
    {
        var books = await _repository.GetAllAsync();

        // --- SPECIFICATION PATTERN ---
        // Build a chain of specifications based on the active filters.
        // Each spec encapsulates one rule; AndSpecification composes them.
        ISpecification<Book>? spec = null;

        if (!string.IsNullOrWhiteSpace(query))
            spec = new TitleOrAuthorContainsSpecification(query);

        if (status.HasValue)
        {
            ISpecification<Book> statusSpec = new StatusSpecification(status.Value);
            // Compose: if a text spec already exists, combine with AND.
            // AndSpecification itself implements ISpecification<Book> —
            // this is the Composite pattern: a spec made of other specs.
            spec = spec is null
                ? statusSpec
                : new AndSpecification<Book>(spec, statusSpec);
        }

        if (spec is not null)
            books = books.Where(spec.IsSatisfiedBy).ToList();

        // --- STRATEGY PATTERN ---
        // Select the sort strategy by name, defaulting to the first registered.
        // The service has zero knowledge of how any strategy sorts —
        // it calls Sort() and lets polymorphism dispatch to the right class.
        var strategy = _sortStrategies.FirstOrDefault(s => s.Name == sortStrategyName)
                       ?? _sortStrategies.First();

        return strategy.Sort(books).ToList();
    }

    /// <inheritdoc/>
    public async Task<DTOs.Common.PaginatedResult<Book>> GetPagedAsync(DTOs.Book.PagedBooksRequest req)
    {
        // Reuse SearchAsync — apply filters + sort, then paginate in memory.
        ReadingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<ReadingStatus>(req.Status, true, out var s))
            parsedStatus = s;

        var books = await SearchAsync(req.SearchTerm ?? string.Empty, parsedStatus, req.SortBy);

        var totalCount = books.Count;
        var items = books
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

        return new DTOs.Common.PaginatedResult<Book>
        {
            Items      = items,
            TotalCount = totalCount,
            PageNumber = req.PageNumber,
            PageSize   = req.PageSize
        };
    }

    /// <inheritdoc/>
    public Task AddBookAsync(Book book) => _repository.AddAsync(book);

    /// <inheritdoc/>
    public Task UpdateBookAsync(Book book) => _repository.UpdateAsync(book);

    /// <inheritdoc/>
    public Task DeleteBookAsync(int id) => _repository.DeleteAsync(id);
}
