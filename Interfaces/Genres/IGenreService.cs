using BookLibrary.Models;

namespace BookLibrary.Interfaces.Genres;

public interface IGenreService
{
    Task<List<Genre>> GetAllGenresAsync();
    Task<Genre?> GetGenreByIdAsync(int id);
    Task AddGenreAsync(Genre genre);
    Task UpdateGenreAsync(Genre genre);
    Task DeleteGenreAsync(int id);
}