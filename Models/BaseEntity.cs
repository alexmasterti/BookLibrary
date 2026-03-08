namespace BookLibrary.Models;

/// <summary>
/// Abstract base class for all domain entities in the application.
///
/// CONCEPT: Inheritance (OOP Pillar)
///   All persistent entities inherit from this class, so they automatically
///   get an Id and a CreatedAt timestamp without repeating those properties
///   in every class. If you ever need to add an UpdatedAt or a SoftDelete
///   flag to every entity, you add it here once and every class gets it.
///
/// CONCEPT: Abstraction (OOP Pillar)
///   This class is abstract — it cannot be instantiated directly.
///   It exists only to be inherited. It defines a contract: "every entity
///   in this system has an Id and a CreatedAt."
///
/// EF CORE NOTE:
///   Entity Framework Core discovers Id and CreatedAt from this base class
///   automatically. No extra configuration is needed.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key. EF Core recognises the name "Id" by convention and
    /// maps it as the table's primary key automatically.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// UTC timestamp set at construction time. Useful for auditing when
    /// records were created without adding extra migration columns later.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
