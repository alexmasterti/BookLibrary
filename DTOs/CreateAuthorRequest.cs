namespace BookLibrary.DTOs;

/// <summary>
/// Inbound DTO for creating a new author.
///
/// CONCEPT: DTO
///   Separates the API input shape from the domain model.
///   FluentValidation runs against this object before the controller action executes.
/// </summary>
public class CreateAuthorRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int? BirthYear { get; set; }
    public string? Nationality { get; set; }
}
