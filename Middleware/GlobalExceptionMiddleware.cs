using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Middleware;

/// <summary>
/// CONCEPT: Global Exception Handling + ProblemDetails (RFC 7807)
///   Catches all unhandled exceptions and returns structured JSON error responses.
///   Prevents stack traces leaking to clients in production.
///   RFC 7807 format: { type, title, status, detail, traceId }
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            KeyNotFoundException   => (HttpStatusCode.NotFound,           "Resource Not Found"),
            ArgumentException      => (HttpStatusCode.BadRequest,         "Bad Request"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,  "Unauthorized"),
            _                      => (HttpStatusCode.InternalServerError,"An unexpected error occurred")
        };

        var problem = new ProblemDetails
        {
            Status   = (int)statusCode,
            Title    = title,
            Detail   = _env.IsDevelopment() ? ex.Message : "See server logs for details.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        // Use WriteAsync instead of WriteAsJsonAsync for compatibility with TestHost (integration tests)
        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
