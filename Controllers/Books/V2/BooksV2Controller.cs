using Asp.Versioning;
using BookLibrary.DTOs.Book.V2;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers.Books.V2;

/// <summary>
/// API V2 — Enriched book responses with computed fields.
/// Demonstrates backward-compatible API evolution via versioning.
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/books")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class BooksV2Controller : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksV2Controller(IBookService bookService)
        => _bookService = bookService;

    /// <summary>Get all books with enriched V2 response.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookDtoV2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var books = await _bookService.GetAllBooksAsync();
        return Ok(books.Select(ToV2Dto));
    }

    /// <summary>Get a book by ID with enriched V2 response.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookDtoV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        return book is null ? NotFound() : Ok(ToV2Dto(book));
    }

    private static BookDtoV2 ToV2Dto(Book book) => new()
    {
        Id        = book.Id,
        Title     = book.Title,
        Author    = book.Author,
        Genre     = book.Genre,
        Year      = book.Year,
        Status    = book.Status.ToString(),
        CreatedAt = book.CreatedAt
    };
}
