namespace BookLibrary.DTOs.Genre;

public class CreateGenreRequest
{
    public required string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}