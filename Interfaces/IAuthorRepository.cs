using BookLibrary.Models;

namespace BookLibrary.Interfaces;

/// <summary>
/// Author-specific repository interface.
///
/// CONCEPT: Interface Inheritance
///   Extends IRepository&lt;Author&gt; and inherits all five CRUD operations.
///   Author-specific queries (e.g. GetByNationalityAsync) belong here.
///
/// PRINCIPLE: Open/Closed (SOLID — 'O')
///   Add author-specific data operations here without touching
///   IRepository&lt;T&gt; or any other entity's repository contract.
/// </summary>
public interface IAuthorRepository : IRepository<Author>
{
    // Author-specific queries go here as the application grows.
    // Example: Task<List<Author>> GetByNationalityAsync(string nationality);
}
