using Asp.Versioning;
using BookLibrary.DTOs.Author;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers.Authors;

/// <summary>
/// RESTful API for author operations.
/// All endpoints require a valid JWT Bearer token (see AuthController).
///
/// CONCEPT: REST API
///   - GET    /api/authors         → list all authors
///   - GET    /api/authors/{id}    → get one author
///   - POST   /api/authors         → create author (returns 201 + Location)
///   - PUT    /api/authors/{id}    → update author (returns 204)
///   - DELETE /api/authors/{id}    → delete author (returns 204)
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   Controller depends on IAuthorService — never on AuthorService directly.
///
/// CONCEPT: DTO Mapping
///   Domain models (Author) are never returned from the API directly.
///   They are mapped to AuthorDto, giving the API a stable, versioned contract.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    // ── GET /api/authors ─────────────────────────────────────────────────────

    /// <summary>Returns all authors in the library, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuthorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var authors = await _authorService.GetAllAuthorsAsync();
        return Ok(authors.Select(ToDto));
    }

    // ── GET /api/authors/{id} ────────────────────────────────────────────────

    /// <summary>Returns a single author by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var author = await _authorService.GetAuthorByIdAsync(id);
        return author is null ? NotFound() : Ok(ToDto(author));
    }

    // ── POST /api/authors ────────────────────────────────────────────────────

    /// <summary>Creates a new author. Returns 201 with the created resource.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAuthorRequest request)
    {
        // FluentValidation (CreateAuthorValidator) runs automatically before this
        // method is called. If validation fails, a 400 with structured errors is
        // returned before we ever reach here.
        var author = new Author
        {
            Name        = request.Name,
            Bio         = request.Bio,
            BirthYear   = request.BirthYear,
            Nationality = request.Nationality
        };

        await _authorService.AddAuthorAsync(author);

        // 201 Created + Location header pointing to the new resource.
        return CreatedAtAction(nameof(GetById), new { id = author.Id }, ToDto(author));
    }

    // ── PUT /api/authors/{id} ────────────────────────────────────────────────

    /// <summary>Updates an existing author. Returns 204 No Content on success.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAuthorRequest request)
    {
        var existing = await _authorService.GetAuthorByIdAsync(id);
        if (existing is null) return NotFound();

        // Apply the update — only fields exposed in the DTO are changeable.
        existing.Name        = request.Name;
        existing.Bio         = request.Bio;
        existing.BirthYear   = request.BirthYear;
        existing.Nationality = request.Nationality;

        await _authorService.UpdateAuthorAsync(existing);
        return NoContent();
    }

    // ── DELETE /api/authors/{id} ─────────────────────────────────────────────

    /// <summary>Deletes an author by ID. Returns 204 No Content on success.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _authorService.GetAuthorByIdAsync(id);
        if (existing is null) return NotFound();

        await _authorService.DeleteAuthorAsync(id);
        return NoContent();
    }

    // ── DTO Mapping ──────────────────────────────────────────────────────────

    private static AuthorDto ToDto(Author a) =>
        new(a.Id, a.Name, a.Bio, a.BirthYear, a.Nationality, a.CreatedAt);
}
