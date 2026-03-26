using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;

namespace BookLibrary.Strategies;

/// <summary>
/// Sorts books by publication year, newest first.
/// Books with no year recorded appear last.
///
/// PATTERN: Strategy (concrete implementation)
///   See <see cref="TitleSortStrategy"/> for the full pattern explanation.
/// </summary>
public class YearSortStrategy : ISortStrategy<Book>
{
    /// <inheritdoc/>
    public string Name => "Year";

    /// <inheritdoc/>
    public IEnumerable<Book> Sort(IEnumerable<Book> items)
        // Books with null Year are pushed to the end with int.MinValue.
        => items.OrderByDescending(b => b.Year ?? int.MinValue);
}
