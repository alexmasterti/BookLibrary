namespace BookLibrary.Models;

/// <summary>
/// Represents a book in the library.
///
/// CONCEPT: Inheritance (OOP Pillar)
///   Book extends <see cref="BaseEntity"/>, inheriting Id and CreatedAt.
///   It only declares the properties that are specific to a book.
///   If we later add Magazine or Podcast entities, they also inherit
///   from BaseEntity and get the same base fields for free.
///
/// CONCEPT: Encapsulation (OOP Pillar)
///   Properties are exposed via public getters/setters. The internal
///   state is protected from arbitrary manipulation — EF Core and the
///   application interact with the object only through its public API.
/// </summary>
public class Book : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public ReadingStatus Status { get; set; } = ReadingStatus.WantToRead;
}
