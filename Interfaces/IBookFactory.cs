using BookLibrary.Models;

namespace BookLibrary.Interfaces;

/// <summary>
/// Defines a factory for creating <see cref="Book"/> objects.
///
/// PATTERN: Factory
///   The Factory pattern centralises object creation. Instead of calling
///   <c>new Book { Title = ..., Author = ..., Status = ... }</c> in many
///   places, all callers go through the factory. If the creation logic
///   ever changes (e.g., new required field, default value, validation),
///   you change it in one place.
///
/// PRINCIPLE: Dependency Inversion (SOLID — 'D')
///   Code that needs to create books depends on this interface, not on
///   the concrete <c>BookFactory</c>. Tests can inject a fake factory
///   that returns predictable objects.
///
/// PATTERN: Factory + Builder (composition)
///   The concrete implementation (<see cref="BookLibrary.Factories.BookFactory"/>)
///   delegates the actual construction to <c>BookBuilder</c>, demonstrating
///   how two patterns complement each other: the factory decides WHAT
///   to create; the builder decides HOW to assemble it.
/// </summary>
public interface IBookFactory
{
    /// <summary>
    /// Creates a new <see cref="Book"/> with the provided values.
    /// All parameters except title and author are optional and will
    /// use sensible defaults when omitted.
    /// </summary>
    Book Create(
        string title,
        string author,
        string? genre = null,
        int? year = null,
        ReadingStatus status = ReadingStatus.WantToRead);
}
