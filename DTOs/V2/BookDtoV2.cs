namespace BookLibrary.DTOs.V2;

/// <summary>
/// V2 book response — adds computed fields not in V1.
/// Shows how API versioning lets you extend responses without breaking existing clients.
/// </summary>
public class BookDtoV2
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Genre { get; init; }
    public int? Year { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    // V2-only fields — new without breaking V1 clients
    public int DaysInLibrary => (int)(DateTime.UtcNow - CreatedAt).TotalDays;
    public bool IsRecentlyAdded => DaysInLibrary <= 30;
    public string? Era => Year switch
    {
        <= 1900 => "Classic",
        <= 1950 => "Early Modern",
        <= 2000 => "Modern",
        > 2000  => "Contemporary",
        null    => null
    };
}
