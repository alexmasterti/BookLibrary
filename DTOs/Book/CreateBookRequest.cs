namespace BookLibrary.DTOs.Book;

/// <summary>Inbound DTO for POST /api/books.</summary>
public record CreateBookRequest(
    string  Title,
    string  Author,
    string? Genre,
    int?    Year,
    string? Status);
