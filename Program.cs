using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using BookLibrary.BackgroundServices;
using BookLibrary.Data;
using BookLibrary.Factories;
using BookLibrary.Interfaces;
using BookLibrary.Middleware;
using BookLibrary.Models;
using BookLibrary.Options;
using BookLibrary.Repositories;
using BookLibrary.Services;
using BookLibrary.Strategies;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ─── UI Framework ──────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ─── REST API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ─── ProblemDetails (RFC 7807) ──────────────────────────────────────────────
builder.Services.AddProblemDetails();

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

// ─── PATTERN: Factory ──────────────────────────────────────────────────────
builder.Services.AddSingleton<IBookFactory, BookFactory>();

// ─── JWT Token Service ─────────────────────────────────────────────────────
builder.Services.AddSingleton<ITokenService, TokenService>();

// ─── AI Recommendation Service ─────────────────────────────────────────────
builder.Services.AddScoped<IBookRecommendationService, BookRecommendationService>();

// ─── CONCEPT: BackgroundService ────────────────────────────────────────────
builder.Services.AddHostedService<LibraryStatsBackgroundService>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookLibrary API v1");
        c.DocumentTitle = "BookLibrary API";
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
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
