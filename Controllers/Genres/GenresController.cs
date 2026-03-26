using Asp.Versioning;
using BookLibrary.DTOs.Genre;
using BookLibrary.Interfaces.Genres;
using BookLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers.Genres;

/// <summary>
/// RESTful API for Genre operations.
/// All endpoints require a valid JWT Bearer token (see AuthController).
///
/// CONCEPT: REST API
///   - GET    /api/Genres         → list all Genres
///   - GET    /api/Genres/{id}    → get one Genre
///   - POST   /api/Genres         → create Genre (returns 201 + Location)
///   - PUT    /api/Genres/{id}    → update Genre (returns 204)
///   - DELETE /api/Genres/{id}    → delete Genre (returns 204)
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   Controller depends on IGenreService — never on GenreService directly.
///
/// CONCEPT: DTO Mapping
///   Domain models (Genre) are never returned from the API directly.
///   They are mapped to GenreDTO, giving the API a stable, versioned contract.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    // ── GET /api/Genres ─────────────────────────────────────────────────────

    /// <summary>Returns all Genres in the library, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GenreDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var genres = await _genreService.GetAllGenresAsync();
        return Ok(genres.Select(ToDto));
    }

    // ── GET /api/Genres/{id} ────────────────────────────────────────────────

    /// <summary>Returns a single Genre by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GenreDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var genre = await _genreService.GetGenreByIdAsync(id);
        return genre is null ? NotFound() : Ok(ToDto(genre));
    }

    // ── POST /api/Genres ────────────────────────────────────────────────────

    /// <summary>Creates a new Genre. Returns 201 with the created resource.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GenreDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGenreRequest request)
    {
        // FluentValidation (CreateGenreValidator) runs automatically before this
        // method is called. If validation fails, a 400 with structured errors is
        // returned before we ever reach here.
        var genre = new Genre
        {
            Name        = request.Name,
            Description = request.Description,
        };

        await _genreService.AddGenreAsync(genre);

        // 201 Created + Location header pointing to the new resource.
        return CreatedAtAction(nameof(GetById), new { id = genre.Id }, ToDto(genre));
    }

    // ── PUT /api/Genres/{id} ────────────────────────────────────────────────

    /// <summary>Updates an existing Genre. Returns 204 No Content on success.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGenreRequest request)
    {
        var existing = await _genreService.GetGenreByIdAsync(id);
        if (existing is null) return NotFound();

        existing.Name        = request.Name;
        existing.Description = request.Description;

        await _genreService.UpdateGenreAsync(existing);
        return NoContent();
    }

    // ── DELETE /api/Genres/{id} ─────────────────────────────────────────────

    /// <summary>Deletes a Genre by ID. Returns 204 No Content on success.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _genreService.GetGenreByIdAsync(id);
        if (existing is null) return NotFound();

        await _genreService.DeleteGenreAsync(id);
        return NoContent();
    }

    // ── DTO Mapping ──────────────────────────────────────────────────────────

    private static GenreDTO ToDto(Genre g) =>
        new(g.Id, g.Name, g.Description, g.CreatedAt);
}
