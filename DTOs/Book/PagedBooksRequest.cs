namespace BookLibrary.DTOs.Book;

/// <summary>Query parameters for the paginated books endpoint.</summary>
public class PagedBooksRequest
{
    private int _pageSize = 10;

    public int    PageNumber  { get; init; } = 1;
    public int    PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, 50);
    }
    public string? SearchTerm { get; init; }
    public string? Status     { get; init; }
    public string? SortBy     { get; init; }
}
