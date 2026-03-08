namespace BookLibrary.Options;

/// <summary>
/// Configuration options for the in-memory caching layer.
/// Bound from appsettings.json section "Cache".
///
/// CONCEPT: Options Pattern (ASP.NET Core)
///   Strongly-typed configuration objects are injected via
///   IOptions&lt;T&gt; instead of reading raw IConfiguration strings.
///   This makes configuration testable and refactor-friendly.
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>How long the full book list is cached (seconds). Default: 60.</summary>
    public int BooksCacheDurationSeconds { get; set; } = 60;
}
