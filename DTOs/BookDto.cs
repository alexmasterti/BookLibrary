namespace BookLibrary.DTOs;

/// <summary>
/// Outbound DTO — the shape of a book as returned by the API.
///
/// CONCEPT: DTO (Data Transfer Object)
///   DTOs decouple the API surface from the domain model.
///   If Book gains an internal field, the API response doesn't change.
///   If the API needs a formatted field, we don't pollute the domain model.
/// </summary>
public record BookDto(
    int      Id,
    string   Title,
    string   Author,
    string?  Genre,
    int?     Year,
    string   Status,
    DateTime CreatedAt);
