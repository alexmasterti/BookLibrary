using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using BookLibrary.DTOs.Book;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Models;
using BookLibrary.Options;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace BookLibrary.Services.Books;

/// <summary>
/// Uses the Anthropic Claude API to suggest books based on the user's reading history.
///
/// CONCEPT: AI-Augmented Engineering
///   Sends the user's read/reading books to Claude and asks for
///   personalised recommendations in structured JSON format.
///   Falls back gracefully when no API key is configured.
///
/// CONCEPT: Polly Resilience Pipeline
///   External HTTP calls (to Anthropic) can fail for many reasons:
///   network blips, rate limits, server restarts. Polly adds:
///
///   RETRY: If the call fails, try again up to 3 times with exponential backoff
///          (2s, 4s, 8s). Most transient failures resolve within a couple retries.
///
///   CIRCUIT BREAKER: If 50% of calls fail in a 30-second window (min 3 calls),
///          the circuit "opens" — subsequent calls fail immediately for 30 seconds
///          rather than hammering a failing service. This protects both the caller
///          and the downstream service from cascading failures.
///
///   TIMEOUT: Any single attempt that takes more than 15 seconds is cancelled.
///          Without this, a hung HTTP call would hold a thread forever.
/// </summary>
public class BookRecommendationService : IBookRecommendationService
{
    private readonly AnthropicOptions _options;
    private readonly ILogger<BookRecommendationService> _logger;

    // CONCEPT: Static resilience pipeline (shared across all instances)
    //   This is static so the circuit breaker state is shared — if one request
    //   opens the circuit, all subsequent requests also fail fast.
    private static readonly ResiliencePipeline _resiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 3,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(15))
        .Build();

    public BookRecommendationService(
        IOptions<AnthropicOptions> options,
        ILogger<BookRecommendationService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<BookRecommendationResult> GetRecommendationsAsync(
        IEnumerable<Book> readBooks,
        int count = 5)
    {
        var bookList = readBooks.ToList();

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Anthropic API key not configured.");
            return new BookRecommendationResult
            {
                Reasoning = "AI recommendations are not configured. Add your Anthropic API key to appsettings.json under 'Anthropic:ApiKey'."
            };
        }

        if (!bookList.Any())
        {
            return new BookRecommendationResult
            {
                Reasoning = "Add some books to your reading list first, then come back for personalised recommendations!"
            };
        }

        try
        {
            var client = new AnthropicClient(_options.ApiKey);

            var booksSummary = new StringBuilder();
            foreach (var book in bookList.Take(20))
            {
                booksSummary.AppendLine(
                    $"- \"{book.Title}\" by {book.Author}" +
                    (book.Genre is not null ? $" ({book.Genre})" : "") +
                    (book.Year.HasValue    ? $" [{book.Year}]"   : "") +
                    $" — {book.Status}");
            }

            var jsonFormat =
                "{\n" +
                "  \"reasoning\": \"Brief explanation of their reading taste (1-2 sentences)\",\n" +
                "  \"recommendations\": [\n" +
                "    {\n" +
                "      \"title\": \"Book Title\",\n" +
                "      \"author\": \"Author Name\",\n" +
                "      \"genre\": \"Genre\",\n" +
                "      \"year\": 2020,\n" +
                "      \"reason\": \"Why this book matches their taste (1 sentence)\"\n" +
                "    }\n" +
                "  ]\n" +
                "}";

            var prompt =
                $"You are a knowledgeable book recommendation engine. " +
                $"Based on the user's reading history below, suggest {count} books they would enjoy.\n\n" +
                $"USER'S READING HISTORY:\n{booksSummary}\n\n" +
                "Respond ONLY with valid JSON in this exact format (no markdown, no extra text):\n" +
                jsonFormat + "\n\n" +
                "Rules:\n" +
                "- Recommend books NOT already in their list\n" +
                "- year can be null if unknown\n" +
                "- Keep reason concise and specific to their reading taste";

            var parameters = new MessageParameters
            {
                Model     = _options.Model,
                MaxTokens = 1024,
                Messages  = new List<Message>
                {
                    new Message(RoleType.User, prompt)
                }
            };

            BookRecommendationResult? result = null;

            // CONCEPT: Polly ExecuteAsync — wraps the call with retry + circuit breaker + timeout
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var response = await client.Messages.GetClaudeMessageAsync(parameters);
                var raw = response.Message.ToString()?.Trim() ?? string.Empty;

                // Strip markdown code fences if model wrapped response in ```json
                if (raw.StartsWith("```"))
                {
                    var lines = raw.Split('\n').ToList();
                    raw = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```")));
                }

                var parsed = JsonSerializer.Deserialize<ClaudeRecommendationResponse>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is null)
                    throw new InvalidOperationException("Failed to parse Claude response.");

                result = new BookRecommendationResult
                {
                    Reasoning       = parsed.Reasoning ?? string.Empty,
                    Recommendations = parsed.Recommendations?.Select(r => new RecommendedBook
                    {
                        Title  = r.Title  ?? string.Empty,
                        Author = r.Author ?? string.Empty,
                        Genre  = r.Genre,
                        Year   = r.Year,
                        Reason = r.Reason ?? string.Empty
                    }).ToList() ?? new()
                };
            }, CancellationToken.None);

            return result ?? new BookRecommendationResult
            {
                Reasoning = "Unable to fetch recommendations right now. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API for book recommendations.");
            return new BookRecommendationResult
            {
                Reasoning = "Unable to fetch recommendations right now. Please try again later."
            };
        }
    }

    // ── Internal deserialization models ──────────────────────────────────────
    private class ClaudeRecommendationResponse
    {
        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }

        [JsonPropertyName("recommendations")]
        public List<ClaudeBook>? Recommendations { get; set; }
    }

    private class ClaudeBook
    {
        [JsonPropertyName("title")]  public string? Title  { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("genre")]  public string? Genre  { get; set; }
        [JsonPropertyName("year")]   public int?    Year   { get; set; }
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }
}
