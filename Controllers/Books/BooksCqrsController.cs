using BookLibrary.CQRS.Commands;
using BookLibrary.CQRS.Queries;
using BookLibrary.DTOs.Book;
using BookLibrary.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers.Books;

/// <summary>
/// CQRS-style API endpoint using MediatR.
///
/// CONCEPT: CQRS (Command Query Responsibility Segregation)
///   Instead of injecting IBookService directly, this controller sends
///   Commands and Queries through IMediator — a message bus.
///   MediatR finds the right Handler and executes it.
///
///   This decouples the controller from business logic completely.
///   The controller only knows WHAT to do (the command/query), not HOW.
///
/// Route: /api/books-cqrs (alongside existing /api/books for comparison)
/// </summary>
[ApiController]
[Route("api/books-cqrs")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class BooksCqrsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BooksCqrsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all books via CQRS Query.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetAllBooksQuery()));

    /// <summary>Get book by ID via CQRS Query.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetBookByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Search books via CQRS Query.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string search = "",
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null)
    {
        ReadingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReadingStatus>(status, true, out var s))
            parsedStatus = s;

        return Ok(await _mediator.Send(new SearchBooksQuery(search, parsedStatus, sortBy)));
    }

    /// <summary>Create a book via CQRS Command.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateBookCommand command)
        => Ok(await _mediator.Send(command));

    /// <summary>Delete a book via CQRS Command.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteBookCommand(id));
        return NoContent();
    }
}
