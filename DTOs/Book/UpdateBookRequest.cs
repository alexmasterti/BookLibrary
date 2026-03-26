namespace BookLibrary.DTOs.Book;

/// <summary>Inbound DTO for PUT /api/books/{id}.</summary>
public record UpdateBookRequest(
    string  Title,
    string  Author,
    string? Genre,
    int?    Year,
    string  Status);
