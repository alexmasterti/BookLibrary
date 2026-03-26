namespace BookLibrary.DTOs.Book;

public class BookRecommendationResult
{
    public List<RecommendedBook> Recommendations { get; init; } = new();
    public string Reasoning { get; init; } = string.Empty;
}

public class RecommendedBook
{
    public string  Title   { get; init; } = string.Empty;
    public string  Author  { get; init; } = string.Empty;
    public string? Genre   { get; init; }
    public int?    Year    { get; init; }
    public string  Reason  { get; init; } = string.Empty;
}
