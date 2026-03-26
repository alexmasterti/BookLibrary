using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;

namespace BookLibrary.Specifications;

/// <summary>
/// Composite specification satisfied only when BOTH inner specifications
/// are satisfied simultaneously.
///
/// PATTERN: Composite Specification
///   Combines the Specification pattern with the Composite pattern.
///   AndSpecification itself implements ISpecification&lt;T&gt;, so it can
///   be wrapped inside yet another AndSpecification to build arbitrarily
///   complex rules from simple ones:
///
///   <code>
///   var spec = new AndSpecification&lt;Book&gt;(
///       new TitleOrAuthorContainsSpecification("Martin"),
///       new StatusSpecification(ReadingStatus.Read)
///   );
///   // Finds books by "Martin" that have been read.
///   </code>
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   IsSatisfiedBy calls _left.IsSatisfiedBy and _right.IsSatisfiedBy.
///   Neither AndSpecification nor its callers know which concrete
///   specification is stored — the correct implementation runs via
///   polymorphic dispatch.
///
/// CONCEPT: Open/Closed Principle (SOLID — 'O')
///   Adding an OrSpecification or NotSpecification follows the same
///   pattern without touching any existing specification code.
/// </summary>
/// <typeparam name="T">The entity type both inner specifications evaluate.</typeparam>
public class AndSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    /// <param name="left">First rule — must be satisfied.</param>
    /// <param name="right">Second rule — must also be satisfied.</param>
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    /// <inheritdoc/>
    /// Uses short-circuit evaluation: if _left fails, _right is never checked.
    public bool IsSatisfiedBy(T entity)
        => _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
}
