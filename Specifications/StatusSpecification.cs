using BookLibrary.Interfaces;
using BookLibrary.Models;

namespace BookLibrary.Specifications;

/// <summary>
/// Specification that is satisfied when a book's Status matches exactly.
///
/// PATTERN: Specification (concrete implementation)
///   See <see cref="TitleOrAuthorContainsSpecification"/> for the full
///   pattern explanation.
/// </summary>
public class StatusSpecification : ISpecification<Book>
{
    private readonly ReadingStatus _status;

    /// <param name="status">The exact status a book must have to satisfy this rule.</param>
    public StatusSpecification(ReadingStatus status)
    {
        _status = status;
    }

    /// <inheritdoc/>
    public bool IsSatisfiedBy(Book book) => book.Status == _status;
}
