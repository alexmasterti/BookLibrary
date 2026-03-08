using BookLibrary.Models;

namespace BookLibrary.Interfaces;

/// <summary>
/// Book-specific repository interface.
///
/// CONCEPT: Interface Inheritance
///   Extends IRepository&lt;Book&gt; and inherits all five CRUD operations
///   without redeclaring them. Book-specific queries (beyond basic CRUD)
///   belong here, keeping the generic interface clean.
///
/// CONCEPT: Open/Closed Principle (SOLID — 'O')
///   New book-specific data operations can be added here without
///   changing the generic IRepository&lt;T&gt; or any other entity's repository.
/// </summary>
public interface IBookRepository : IRepository<Book>
{
    // Book-specific data access methods go here as the application grows.
    // Example: Task<List<Book>> GetByGenreAsync(string genre);
}
