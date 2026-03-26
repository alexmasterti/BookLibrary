using BookLibrary.DTOs.Book;
using BookLibrary.Models;

namespace BookLibrary.Interfaces.Books;

public interface IBookRecommendationService
{
    Task<BookRecommendationResult> GetRecommendationsAsync(
        IEnumerable<Book> readBooks,
        int count = 5);
}
