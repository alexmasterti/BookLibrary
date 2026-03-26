using BookLibrary.Interfaces.Genres;
using BookLibrary.Models;

namespace BookLibrary.Services.Genres;

public class GenreService : IGenreService
{
    private readonly IGenreRepository _repository;

    public GenreService(IGenreRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Genre>> GetAllGenresAsync() => await _repository.GetAllAsync();

    public async Task<Genre?> GetGenreByIdAsync(int id) => await _repository.GetByIdAsync(id);

    public Task AddGenreAsync(Genre genre) => _repository.AddAsync(genre);

    public Task UpdateGenreAsync(Genre genre) => _repository.UpdateAsync(genre);

    public Task DeleteGenreAsync(int id) => _repository.DeleteAsync(id);
}