using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Options;
using Microsoft.Extensions.Options;

namespace BookLibrary.BackgroundServices;

/// <summary>
/// A hosted background service that periodically logs library statistics.
///
/// CONCEPT: BackgroundService / IHostedService
///   BackgroundService is a base class for long-running background tasks
///   that start with the application and stop on shutdown.
///   ExecuteAsync runs in the background thread pool — it does not block
///   the request pipeline.
///
/// CRITICAL PITFALL — Scoped services in a Singleton:
///   AddHostedService&lt;T&gt; registers the service as a Singleton.
///   IBookService is registered as Scoped (one per request/circuit).
///   You CANNOT inject a Scoped service into a Singleton constructor —
///   this would capture a single scope forever, leaking DbContext.
///
///   SOLUTION: Inject IServiceScopeFactory (Singleton-safe) and create
///   a new scope on every timer tick. The scope is disposed after use,
///   releasing the DbContext and all scoped dependencies cleanly.
///
/// PRINCIPLE: Single Responsibility
///   This class has one job: report stats on a schedule.
///   It delegates data retrieval to IBookService.
/// </summary>
public class LibraryStatsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LibraryStatsBackgroundService> _logger;
    private readonly LibraryStatsOptions _options;

    public LibraryStatsBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LibraryStatsBackgroundService> logger,
        IOptions<LibraryStatsOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _options      = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[LibraryStats] Background service started. Reporting every {Seconds}s.",
            _options.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait first, then report. This avoids a report at t=0 before
            // the database is fully seeded on first startup.
            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);

            await LogStatsAsync(stoppingToken);
        }
    }

    private async Task LogStatsAsync(CancellationToken ct)
    {
        try
        {
            // Create a fresh scope per tick — this resolves IBookService
            // as Scoped (with its own DbContext), uses it, then disposes cleanly.
            await using var scope = _scopeFactory.CreateAsyncScope();
            var bookService = scope.ServiceProvider.GetRequiredService<IBookService>();
            var books       = await bookService.GetAllBooksAsync();

            var counts = books
                .GroupBy(b => b.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            _logger.LogInformation(
                "[LibraryStats] Total={Total} | WantToRead={WantToRead} | Reading={Reading} | Finished={Finished}",
                books.Count,
                counts.GetValueOrDefault(ReadingStatus.WantToRead),
                counts.GetValueOrDefault(ReadingStatus.CurrentlyReading),
                counts.GetValueOrDefault(ReadingStatus.Read));
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation — do not swallow it.
            throw;
        }
        catch (Exception ex)
        {
            // Log but continue — a single failure should not stop the service.
            _logger.LogError(ex, "[LibraryStats] Failed to collect statistics");
        }
    }
}
