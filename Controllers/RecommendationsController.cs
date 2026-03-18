using BookLibrary.DTOs;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers;

/// <summary>
/// Exposes AI-powered book recommendations via the REST API.
/// Uses the user's read/currently-reading books as input to Claude.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class RecommendationsController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBookRecommendationService _recommendationService;

    public RecommendationsController(
        IBookService bookService,
        IBookRecommendationService recommendationService)
    {
        _bookService           = bookService;
        _recommendationService = recommendationService;
    }

    /// <summary>
    /// Returns AI-powered book recommendations based on reading history.
    /// Reads and currently-reading books are used as input.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BookRecommendationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations([FromQuery] int count = 5)
    {
        var allBooks  = await _bookService.GetAllBooksAsync();
        var readBooks = allBooks
            .Where(b => b.Status is ReadingStatus.Read or ReadingStatus.CurrentlyReading)
            .ToList();

        var result = await _recommendationService.GetRecommendationsAsync(readBooks, count);
        return Ok(result);
    }
}
