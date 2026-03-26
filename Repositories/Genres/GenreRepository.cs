using BookLibrary.Data;
using BookLibrary.Interfaces.Genres;
using BookLibrary.Models;
using BookLibrary.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories.Genres;

public class GenreRepository : Repository<Genre>, IGenreRepository
{
    public GenreRepository(AppDbContext context) : base(context){ }

    public override async Task<List<Genre>> GetAllAsync() => await _context.Set<Genre>().OrderBy(g => g.Name).ToListAsync();

}