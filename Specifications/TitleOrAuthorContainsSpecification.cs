using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;

namespace BookLibrary.Specifications;

/// <summary>
/// Specification that is satisfied when a book's Title or Author
/// contains the search term (case-insensitive).
///
/// PATTERN: Specification (concrete implementation)
///   This encapsulates one specific filtering rule as a named, reusable
///   object. The same specification can be used in any service, query,
///   or test that needs to check whether a book matches a text search —
///   no copy-pasting filter lambdas across the codebase.
///
/// CONCEPT: Single Responsibility (SOLID — 'S')
///   This class does exactly one thing: evaluate the text-match rule.
/// </summary>
public class TitleOrAuthorContainsSpecification : ISpecification<Book>
{
    private readonly string _term;

    /// <param name="term">The text to search for within Title or Author.</param>
    public TitleOrAuthorContainsSpecification(string term)
    {
        _term = term;
    }

    /// <inheritdoc/>
    public bool IsSatisfiedBy(Book book)
        => book.Title.Contains(_term, StringComparison.OrdinalIgnoreCase) ||
           book.Author.Contains(_term, StringComparison.OrdinalIgnoreCase);
}
