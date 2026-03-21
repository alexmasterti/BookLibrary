namespace BookLibrary.Models;

/// <summary>
/// Represents an author in the library.
///
/// CONCEPT: Inheritance (OOP Pillar)
///   Author extends <see cref="BaseEntity"/>, inheriting Id and CreatedAt.
///   Only author-specific properties are declared here.
///
/// CONCEPT: Encapsulation (OOP Pillar)
///   All properties are exposed via public getters/setters.
///   EF Core uses these to map to the underlying Authors table.
/// </summary>
public class Author : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int? BirthYear { get; set; }
    public string? Nationality { get; set; }
}
