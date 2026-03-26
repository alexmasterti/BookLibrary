using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;

namespace BookLibrary.Strategies;

/// <summary>
/// Sorts books alphabetically by Title (A → Z).
///
/// PATTERN: Strategy (concrete implementation)
///   This is one member of the sort strategy family.
///   It implements <see cref="ISortStrategy{T}"/> so it can be injected
///   anywhere a sort strategy is expected, interchangeably with
///   <see cref="AuthorSortStrategy"/> or <see cref="YearSortStrategy"/>.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   BookService holds an ISortStrategy&lt;Book&gt; reference at runtime.
///   When Sort() is called, .NET dispatches to this specific implementation
///   without BookService knowing which concrete class is active.
///
/// CONCEPT: Open/Closed Principle (SOLID — 'O')
///   Adding a new sort order (e.g., by Genre) requires adding one new
///   class and registering it in DI. Zero changes to BookService or the UI.
/// </summary>
public class TitleSortStrategy : ISortStrategy<Book>
{
    /// <inheritdoc/>
    public string Name => "Title";

    /// <inheritdoc/>
    public IEnumerable<Book> Sort(IEnumerable<Book> items)
        => items.OrderBy(b => b.Title);
}
