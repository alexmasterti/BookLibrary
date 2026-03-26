namespace BookLibrary.DTOs.Author;

/// <summary>
/// Inbound DTO for updating an existing author.
///
/// CONCEPT: DTO
///   A separate DTO for updates lets us validate differently from creates.
///   For example, a future PATCH endpoint might allow partial updates
///   without this class needing any changes.
/// </summary>
public class UpdateAuthorRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int? BirthYear { get; set; }
    public string? Nationality { get; set; }
}
