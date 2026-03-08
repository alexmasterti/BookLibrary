using System.Diagnostics;

namespace BookLibrary.Middleware;

/// <summary>
/// Custom ASP.NET Core middleware that measures and logs the elapsed time
/// for every HTTP request.
///
/// CONCEPT: Middleware Pipeline
///   ASP.NET Core processes requests through a pipeline of middleware components.
///   Each middleware receives the request, can do work before and after calling
///   the next component, and can short-circuit the pipeline.
///
///   Request  →  [Middleware A]  →  [Middleware B]  →  [Controller/Blazor]
///   Response ←  [Middleware A]  ←  [Middleware B]  ←  [Controller/Blazor]
///
/// CONCEPT: Constructor vs InvokeAsync injection
///   - Services injected in the CONSTRUCTOR are resolved once (singleton-safe).
///   - Services injected in INVOKEASM parameters are resolved per-request (scoped-safe).
///   ILogger&lt;T&gt; is singleton-safe, so it goes in the constructor.
///
/// PRINCIPLE: Single Responsibility
///   This class does one thing: measure time. It does not touch business logic.
/// </summary>
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip Blazor SignalR connections — they are long-lived websockets
        // and their "elapsed" time would be meaningless (hours, not ms).
        if (context.Request.Path.StartsWithSegments("/_blazor"))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            // Response.StatusCode is only available AFTER _next completes.
            _logger.LogInformation(
                "[Timing] {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
