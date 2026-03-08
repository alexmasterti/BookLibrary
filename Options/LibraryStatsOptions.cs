namespace BookLibrary.Options;

/// <summary>
/// Configuration options for the library stats background service.
/// Bound from appsettings.json section "LibraryStats".
/// </summary>
public class LibraryStatsOptions
{
    public const string SectionName = "LibraryStats";

    /// <summary>How often to log library statistics (seconds). Default: 300 (5 minutes).</summary>
    public int IntervalSeconds { get; set; } = 300;
}
