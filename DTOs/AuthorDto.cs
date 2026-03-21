namespace BookLibrary.DTOs;

/// <summary>
/// Outbound DTO — the shape of an author as returned by the API.
///
/// CONCEPT: DTO (Data Transfer Object)
///   Decouples the API contract from the domain model.
///   Changes to the Author domain model don't automatically change the API response.
/// </summary>
public record AuthorDto(
    int      Id,
    string   Name,
    string?  Bio,
    int?     BirthYear,
    string?  Nationality,
    DateTime CreatedAt);
