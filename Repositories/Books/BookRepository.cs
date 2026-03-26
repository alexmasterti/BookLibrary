using BookLibrary.Data;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;
using BookLibrary.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories.Books;

/// <summary>
/// EF Core repository for Book entities.
///
/// CONCEPT: Inheritance (OOP Pillar)
///   Extends Repository&lt;Book&gt; and inherits all five CRUD operations
///   without writing them again. Only overrides GetAllAsync to add
///   book-specific ordering by title at the database level.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   GetAllAsync is overridden here. When IBookRepository.GetAllAsync
///   is called, .NET dispatches to this override, not the base.
///   The caller (BookService) is completely unaware of this detail.
///
/// PATTERN: Repository
///   Provides a clean data-access layer for books. All EF Core,
///   SQLite, and LINQ-to-SQL concerns are confined to this class
///   and its base. The service layer stays infrastructure-agnostic.
/// </summary>
public class BookRepository : Repository<Book>, IBookRepository
{
    public BookRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Returns all books ordered alphabetically by title.
    /// Overrides the base to push sorting to the database query
    /// (more efficient than sorting in memory after retrieval).
    /// </summary>
    public override async Task<List<Book>> GetAllAsync()
        => await _context.Books.OrderBy(b => b.Title).ToListAsync();
}
