using BookLibrary.Interfaces;
using BookLibrary.Models;

namespace BookLibrary.Strategies;

/// <summary>
/// Sorts books alphabetically by Author name (A → Z).
///
/// PATTERN: Strategy (concrete implementation)
///   See <see cref="TitleSortStrategy"/> for the full pattern explanation.
///   This class is interchangeable with all other ISortStrategy&lt;Book&gt;
///   implementations.
/// </summary>
public class AuthorSortStrategy : ISortStrategy<Book>
{
    /// <inheritdoc/>
    public string Name => "Author";

    /// <inheritdoc/>
    public IEnumerable<Book> Sort(IEnumerable<Book> items)
        => items.OrderBy(b => b.Author);
}
