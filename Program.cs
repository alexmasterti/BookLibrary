using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using MediatR;
using Microsoft.OpenApi.Models;
using BookLibrary.BackgroundServices;
using BookLibrary.Data;
using BookLibrary.Factories;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using BookLibrary.Middleware;
using BookLibrary.Models;
using BookLibrary.Options;
using BookLibrary.Repositories.Books;
using BookLibrary.Repositories.Authors;
using BookLibrary.Repositories.Common;
using BookLibrary.Services.Books;
using BookLibrary.Services.Authors;
using BookLibrary.Services.Auth;
using BookLibrary.Services.Common;
using BookLibrary.Strategies;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// ─── Serilog Bootstrap Logger ──────────────────────────────────────────────
// CONCEPT: Serilog structured logging
//   Replaces the default Microsoft console logger with Serilog.
//   Serilog outputs structured log events — each field (timestamp, level,
//   message, exception) is a typed property, not just a string.
//   This makes logs searchable and filterable in tools like Seq, Splunk, or Datadog.
//   We initialize a bootstrap logger here so that even startup errors are captured.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/booklibrary-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog();

// ─── UI Framework ──────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ─── REST API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ─── API Versioning ─────────────────────────────────────────────────────────
// CONCEPT: API Versioning
//   Allows the API to evolve without breaking existing clients.
//   V1 clients continue to hit /api/v1/books and get the same response.
//   V2 adds enriched fields (DaysInLibrary, IsRecentlyAdded, Era) without touching V1.
//   Versioning is read from: URL segment (/api/v1/), header (X-API-Version), or query (?api-version=1.0)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version"),
        new QueryStringApiVersionReader("api-version")
    );
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ─── ProblemDetails (RFC 7807) ──────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ─── Response Compression ───────────────────────────────────────────────────
// CONCEPT: Response Compression
//   Reduces the size of HTTP responses before sending to the client.
//   Brotli (br) is newer and more efficient than Gzip — 15-25% better compression.
//   All modern browsers support both. The client advertises support via Accept-Encoding header.
//   Enabled for HTTPS too (safe because BREACH attack mitigations are in place for auth tokens).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
    o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o =>
    o.Level = System.IO.Compression.CompressionLevel.SmallestSize);

// ─── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=books.db"));

// ─── Caching ───────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

// ─── Options ───────────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<LibraryStatsOptions>(
    builder.Configuration.GetSection(LibraryStatsOptions.SectionName));
builder.Services.Configure<AnthropicOptions>(
    builder.Configuration.GetSection(AnthropicOptions.SectionName));

// ─── JWT Authentication ─────────────────────────────────────────────────────
var jwtSettings = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidateAudience         = true,
            ValidAudience            = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── FluentValidation ──────────────────────────────────────────────────────
// CONCEPT: FluentValidation
//   Validators are discovered automatically from the assembly.
//   Invalid requests are rejected with 400 + structured error messages
//   before they ever reach controller action methods.
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ─── Rate Limiting ─────────────────────────────────────────────────────────
// CONCEPT: Rate Limiting
//   Protects endpoints from abuse. Fixed window = N requests per time window.
//   'api'  — 100 req/min per IP for all book endpoints
//   'auth' — 10 req/min per IP for the login endpoint (brute-force protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit         = 100;
        o.Window              = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });

    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit         = 10;
        o.Window              = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            title  = "Too Many Requests",
            detail = "Rate limit exceeded. Please wait before retrying.",
            retryAfterSeconds = 60
        }, cancellationToken: cancellationToken);
    };
});

// ─── Health Checks ─────────────────────────────────────────────────────────
// CONCEPT: Health Checks
//   Exposes /health for load balancers and /health/detail for dashboards.
//   DbContextCheck confirms the database is reachable on every probe.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ─── OpenTelemetry ─────────────────────────────────────────────────────────
// CONCEPT: OpenTelemetry — The 3 Pillars of Observability
//   Logs    = what happened (Serilog handles this above)
//   Metrics = how much / how often (request counts, duration histograms, error rates)
//   Traces  = the full journey of ONE request across services (like a call stack in time)
//
//   In a microservices system, one user request might touch 10 services.
//   OpenTelemetry lets you see the whole journey in one distributed trace.
//   The ConsoleExporter is for development — in production swap for OTLP → Jaeger/Zipkin/Datadog.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "BookLibrary", serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            // Don't trace health check calls — they're too noisy
            options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())  // In production: swap for OTLP → Jaeger/Zipkin/Datadog
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// ─── Swagger / OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "BookLibrary API",
        Version = "v1",
        Description = "REST API for the BookLibrary application. " +
                      "Login via POST /api/auth/login to get a JWT token, " +
                      "then click 'Authorize' and enter: Bearer &lt;token&gt;"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token. Example: Bearer eyJhbGci..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── PATTERN: Strategy ─────────────────────────────────────────────────────
builder.Services.AddScoped<ISortStrategy<Book>, TitleSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, AuthorSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, YearSortStrategy>();

// ─── PATTERN: Decorator (three-layer chain) ────────────────────────────────
builder.Services.AddScoped<BookRepository>();

builder.Services.AddScoped<LoggingBookRepository>(sp =>
    new LoggingBookRepository(
        sp.GetRequiredService<BookRepository>(),
        sp.GetRequiredService<ILogger<LoggingBookRepository>>()));

builder.Services.AddScoped<IBookRepository>(sp =>
    new CachingBookRepository(
        sp.GetRequiredService<LoggingBookRepository>(),
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<IOptions<CacheOptions>>(),
        sp.GetRequiredService<ILogger<CachingBookRepository>>()));

// ─── PRINCIPLE: Dependency Inversion (SOLID — 'D') ────────────────────────
builder.Services.AddScoped<IBookService, BookService>();

// ─── Author CRUD ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IAuthorService, AuthorService>();

// ─── PATTERN: Factory ──────────────────────────────────────────────────────
builder.Services.AddSingleton<IBookFactory, BookFactory>();

// ─── JWT Token Service ─────────────────────────────────────────────────────
builder.Services.AddSingleton<ITokenService, TokenService>();

// ─── AI Recommendation Service ─────────────────────────────────────────────
builder.Services.AddScoped<IBookRecommendationService, BookRecommendationService>();

// ─── CONCEPT: BackgroundService ────────────────────────────────────────────
builder.Services.AddHostedService<LibraryStatsBackgroundService>();

// ─── PATTERN: CQRS with MediatR ────────────────────────────────────────────
// CONCEPT: MediatR + CQRS
//   MediatR is a mediator/message-bus for .NET. Instead of controllers
//   calling services directly, they send Commands and Queries through IMediator.
//   MediatR finds the right IRequestHandler<TRequest, TResponse> and executes it.
//   This decouples controllers from business logic completely.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Auto-create the SQLite schema on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ─── Global Exception Handler (FIRST in pipeline) ─────────────────────────
// Must be registered before all other middleware so it catches everything.
app.UseGlobalExceptionHandler();

// ─── Response Compression (early in pipeline, before static files) ──────────
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    // Scalar API Reference — modern, beautiful alternative to Swagger UI
    // Accessible at: /scalar/v1
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.Title = "BookLibrary API";
        options.Theme = ScalarTheme.DeepSpace;
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ─── Serilog Request Logging ────────────────────────────────────────────────
// Replaces verbose ASP.NET Core request logs with a single structured line per request.
// Format: "GET /api/books responded 200 in 12.3ms"
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0}ms";
});

app.UseRouting();

// ─── Rate Limiting (after routing, before auth) ────────────────────────────
app.UseRateLimiter();

// CRITICAL ORDER: Authentication must precede Authorization.
app.UseAuthentication();
app.UseAuthorization();

// ─── Custom Middleware ─────────────────────────────────────────────────────
app.UseRequestTiming();

// ─── Health Checks ─────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status  = report.Status.ToString(),
            checks  = report.Entries.Select(e => new
            {
                name     = e.Key,
                status   = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds + "ms",
                description = e.Value.Description
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds + "ms"
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

// Required to make Program class accessible to integration tests
public partial class Program { }
