namespace BookLibrary.Interfaces;

/// <summary>
/// Defines the Specification pattern contract.
///
/// PATTERN: Specification
///   A Specification turns a business rule into a reusable, named object.
///   Instead of writing filtering logic inline everywhere, you express
///   each rule as a separate class and combine them as needed.
///
///   Without Specification:
///     books.Where(b => b.Title.Contains(q) || b.Author.Contains(q))
///           .Where(b => b.Status == status)
///     ← logic is scattered, not reusable, hard to test in isolation.
///
///   With Specification:
///     var spec = new AndSpecification(new TitleOrAuthorSpec(q), new StatusSpec(s));
///     books.Where(spec.IsSatisfiedBy)
///     ← each rule is named, testable, and composable.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   AndSpecification holds two ISpecification&lt;T&gt; references and calls
///   IsSatisfiedBy on each. It does not know or care which concrete
///   specification it wraps — classic polymorphic dispatch.
///
/// CONCEPT: Interface Segregation (SOLID — 'I')
///   This interface has exactly one method. It does one thing well.
/// </summary>
/// <typeparam name="T">The type of entity this specification evaluates.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Returns <c>true</c> if the given entity satisfies this specification's rule.
    /// </summary>
    bool IsSatisfiedBy(T entity);
}
