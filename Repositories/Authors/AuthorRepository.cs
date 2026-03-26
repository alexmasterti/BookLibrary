using BookLibrary.Data;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;
using BookLibrary.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories.Authors;

/// <summary>
/// EF Core repository for Author entities.
///
/// CONCEPT: Inheritance (OOP Pillar)
///   Extends Repository&lt;Author&gt; and inherits all five CRUD operations.
///   Only overrides GetAllAsync to sort authors alphabetically by name.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   When IAuthorRepository.GetAllAsync is called, .NET dispatches to
///   this override — the caller is completely unaware of this detail.
///
/// PATTERN: Repository
///   All EF Core, SQLite, and LINQ-to-SQL concerns are confined here.
///   The service layer stays infrastructure-agnostic.
/// </summary>
public class AuthorRepository : Repository<Author>, IAuthorRepository
{
    public AuthorRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Returns all authors ordered alphabetically by name.
    /// Sorting is pushed to the database query — more efficient than
    /// fetching all rows and sorting in memory.
    /// </summary>
    public override async Task<List<Author>> GetAllAsync()
        => await _context.Set<Author>().OrderBy(a => a.Name).ToListAsync();
}
