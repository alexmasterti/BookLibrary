namespace BookLibrary.Middleware;

/// <summary>
/// Extension method for registering RequestTimingMiddleware in the pipeline.
///
/// CONCEPT: Extension Methods
///   Conventional ASP.NET Core pattern — middleware registration is
///   exposed as app.UseXxx() to match the framework's own style.
/// </summary>
public static class RequestTimingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTimingMiddleware>();
}
