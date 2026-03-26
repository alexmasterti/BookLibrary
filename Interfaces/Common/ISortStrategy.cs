namespace BookLibrary.Interfaces.Common;

/// <summary>
/// Defines the Strategy pattern contract for sorting a collection.
///
/// PATTERN: Strategy
///   The Strategy pattern defines a family of algorithms, encapsulates
///   each one in its own class, and makes them interchangeable at runtime.
///   The code that uses sorting (BookService) does not need to know
///   which algorithm is active — it just calls Sort().
///
///   Without Strategy:
///     if (sort == "Author") books.OrderBy(b => b.Author)
///     else if (sort == "Year") books.OrderBy(b => b.Year)
///     ...  ← grows forever, hard to extend
///
///   With Strategy:
///     _selectedStrategy.Sort(books)  ← always the same call
///     Adding a new sort = adding one new class, zero changes elsewhere.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   Every concrete strategy (TitleSortStrategy, AuthorSortStrategy, etc.)
///   implements this interface. The caller holds an ISortStrategy&lt;T&gt;
///   reference and calls Sort() — the correct implementation runs at
///   runtime based on which object is assigned. That is polymorphism.
/// </summary>
/// <typeparam name="T">The type of entity being sorted.</typeparam>
public interface ISortStrategy<T>
{
    /// <summary>
    /// Human-readable identifier used to select this strategy by name
    /// (e.g., from a UI dropdown or a query parameter).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies this strategy's sorting algorithm to the collection
    /// and returns the sorted result.
    /// </summary>
    IEnumerable<T> Sort(IEnumerable<T> items);
}
