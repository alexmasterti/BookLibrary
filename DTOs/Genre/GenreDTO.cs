namespace BookLibrary.DTOs.Genre;

public record GenreDTO(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt);
