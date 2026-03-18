using BookLibrary.DTOs;
using BookLibrary.Models;

namespace BookLibrary.Interfaces;

public interface IBookRecommendationService
{
    Task<BookRecommendationResult> GetRecommendationsAsync(
        IEnumerable<Book> readBooks,
        int count = 5);
}
