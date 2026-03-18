using Asp.Versioning;
using BookLibrary.DTOs;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers;

/// <summary>
/// RESTful API for book library operations.
/// All endpoints require a valid JWT Bearer token (see AuthController).
///
/// CONCEPT: REST API
///   Representational State Transfer — a stateless HTTP API where:
///   - GET    retrieves resources (safe, idempotent)
///   - POST   creates a resource (returns 201 Created + Location header)
///   - PUT    replaces a resource fully (idempotent)
///   - DELETE removes a resource (idempotent)
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   This controller depends on IBookService and IBookFactory — never on
///   concrete classes. The same service layer used by Blazor pages is reused
///   here, which means the caching, logging, and business logic work
///   identically from both the UI and the API.
///
/// CONCEPT: DTO mapping
///   Domain models (Book) are never returned directly from the API.
///   They are mapped to BookDto, giving the API its own stable contract.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBookFactory _bookFactory;

    public BooksController(IBookService bookService, IBookFactory bookFactory)
    {
        _bookService = bookService;
        _bookFactory = bookFactory;
    }

    // ── GET /api/books ───────────────────────────────────────────────────────

    /// <summary>Returns all books in the library.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var books = await _bookService.GetAllBooksAsync();
        return Ok(books.Select(ToDto));
    }

    // ── GET /api/books/{id} ──────────────────────────────────────────────────

    /// <summary>Returns a single book by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        return book is null ? NotFound() : Ok(ToDto(book));
    }

    // ── GET /api/books/search ────────────────────────────────────────────────

    /// <summary>Searches books by text, status, and/or sort strategy.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query  = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort   = null)
    {
        ReadingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ReadingStatus>(status, true, out var s))
            parsedStatus = s;

        var books = await _bookService.SearchAsync(query ?? string.Empty, parsedStatus, sort);
        return Ok(books.Select(ToDto));
    }

    // ── GET /api/books/paged ─────────────────────────────────────────────────

    /// <summary>Returns a paginated, filtered, sorted page of books.</summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResult<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedBooksRequest request)
    {
        var paged = await _bookService.GetPagedAsync(request);
        return Ok(new PaginatedResult<BookDto>
        {
            Items      = paged.Items.Select(ToDto),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize   = paged.PageSize
        });
    }

    // ── POST /api/books ──────────────────────────────────────────────────────

    /// <summary>Creates a new book. Returns 201 with the created resource.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Author))
            return BadRequest(new { message = "Title and Author are required." });

        ReadingStatus status = ReadingStatus.WantToRead;
        if (!string.IsNullOrWhiteSpace(request.Status))
            Enum.TryParse(request.Status, true, out status);

        // PATTERN: Factory — creation is delegated to IBookFactory,
        // which internally uses BookBuilder for validation.
        var book = _bookFactory.Create(request.Title, request.Author,
                                       request.Genre, request.Year, status);

        await _bookService.AddBookAsync(book);

        return CreatedAtAction(nameof(GetById), new { id = book.Id }, ToDto(book));
    }

    // ── PUT /api/books/{id} ──────────────────────────────────────────────────

    /// <summary>Updates an existing book. Returns 204 No Content on success.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBookRequest request)
    {
        var existing = await _bookService.GetBookByIdAsync(id);
        if (existing is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Author))
            return BadRequest(new { message = "Title and Author are required." });

        if (!Enum.TryParse<ReadingStatus>(request.Status, true, out var status))
            return BadRequest(new { message = $"Invalid status: {request.Status}" });

        existing.Title  = request.Title;
        existing.Author = request.Author;
        existing.Genre  = request.Genre;
        existing.Year   = request.Year;
        existing.Status = status;

        await _bookService.UpdateBookAsync(existing);
        return NoContent();
    }

    // ── DELETE /api/books/{id} ───────────────────────────────────────────────

    /// <summary>Deletes a book by ID. Returns 204 No Content on success.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _bookService.GetBookByIdAsync(id);
        if (existing is null) return NotFound();

        await _bookService.DeleteBookAsync(id);
        return NoContent();
    }

    // ── DTO Mapping ──────────────────────────────────────────────────────────

    private static BookDto ToDto(Book b) =>
        new(b.Id, b.Title, b.Author, b.Genre, b.Year, b.Status.ToString(), b.CreatedAt);
}
