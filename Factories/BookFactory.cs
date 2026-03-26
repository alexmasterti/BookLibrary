using BookLibrary.Builders;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;

namespace BookLibrary.Factories;

/// <summary>
/// Concrete implementation of <see cref="IBookFactory"/>.
///
/// PATTERN: Factory
///   Centralises Book creation. Callers ask the factory for a book
///   and supply the data; the factory decides how to build it.
///
/// PATTERN: Factory + Builder (composition)
///   This class delegates the actual assembly to <see cref="BookBuilder"/>.
///   The factory owns the creation policy ("what defaults to use"),
///   the builder owns the assembly steps ("how to wire up the object").
///   This is a common real-world combination of the two patterns.
///
/// CONCEPT: Single Responsibility (SOLID — 'S')
///   BookFactory does one thing: create Book instances.
///   It has no knowledge of persistence, UI, or business rules.
///
/// CONCEPT: Polymorphism (OOP Pillar)
///   This class is registered in DI as IBookFactory. Any code that
///   depends on IBookFactory will receive this at runtime without
///   knowing the concrete type — polymorphic substitution.
/// </summary>
public class BookFactory : IBookFactory
{
    /// <inheritdoc/>
    public Book Create(
        string title,
        string author,
        string? genre = null,
        int? year = null,
        ReadingStatus status = ReadingStatus.WantToRead)
    {
        // Delegate assembly to BookBuilder, which validates required fields.
        return new BookBuilder()
            .WithTitle(title)
            .WithAuthor(author)
            .WithGenre(genre)
            .WithYear(year)
            .WithStatus(status)
            .Build();
    }
}
