using System.Diagnostics;

namespace BookLibrary.Services;

/// <summary>
/// Custom ActivitySource for manual distributed tracing.
///
/// CONCEPT: Distributed Tracing
///   A "trace" follows ONE request as it moves through your system.
///   Each step is a "span" — it has a start time, end time, and tags.
///   In a microservices system, one user request might touch 10 services.
///   Tracing lets you see the whole journey in one view.
///
///   ActivitySource is .NET's built-in tracing API (OpenTelemetry-compatible).
///   Activities automatically propagate across async boundaries via AsyncLocalStorage.
/// </summary>
public static class BookLibraryTelemetry
{
    public static readonly ActivitySource Source = new("BookLibrary", "2.0.0");

    public static Activity? StartBookOperation(string operationName, int? bookId = null)
    {
        var activity = Source.StartActivity(operationName, ActivityKind.Internal);
        if (bookId.HasValue)
            activity?.SetTag("book.id", bookId.Value);
        activity?.SetTag("service", "BookLibrary");
        return activity;
    }
}
