namespace BookLibrary.DTOs.Common;

/// <summary>
/// Generic paginated response wrapper.
/// Carries the page data alongside metadata the client needs to render navigation.
/// </summary>
public class PaginatedResult<T>
{
    public IEnumerable<T> Items      { get; init; } = Enumerable.Empty<T>();
    public int TotalCount            { get; init; }
    public int PageNumber            { get; init; }
    public int PageSize              { get; init; }
    public int TotalPages            => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage          => PageNumber < TotalPages;
    public bool HasPreviousPage      => PageNumber > 1;
}
