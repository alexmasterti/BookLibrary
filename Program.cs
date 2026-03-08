using System.Text;
using System.Text.Json.Serialization;
using BookLibrary.BackgroundServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using BookLibrary.Data;
using BookLibrary.Factories;
using BookLibrary.Interfaces;
using BookLibrary.Middleware;
using BookLibrary.Models;
using BookLibrary.Options;
using BookLibrary.Repositories;
using BookLibrary.Services;
using BookLibrary.Strategies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ─── UI Framework ──────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ─── REST API ───────────────────────────────────────────────────────────────
// AddControllers enables Web API controllers alongside Blazor Server.
// JsonStringEnumConverter makes the API return "Read" instead of 2 for enums.
//
// CONCEPT: REST API
//   HTTP endpoints that expose the same business logic as the Blazor UI.
//   Both the UI and the API share the same IBookService — zero duplication.
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ─── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=books.db"));

// ─── Caching ───────────────────────────────────────────────────────────────
// IMemoryCache is a Singleton — one shared cache for the application lifetime.
// CacheOptions binds BooksCacheDurationSeconds from appsettings.json.
//
// CONCEPT: Options Pattern
//   Strongly-typed configuration classes are bound from appsettings.json
//   and injected via IOptions<T> — no raw string lookups in business code.
builder.Services.AddMemoryCache();
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

// ─── Options ───────────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<LibraryStatsOptions>(
    builder.Configuration.GetSection(LibraryStatsOptions.SectionName));

// ─── JWT Authentication ─────────────────────────────────────────────────────
// Protects the REST API layer only. Blazor pages use a separate auth model.
//
// SECURITY NOTE: In production, Jwt:Key MUST come from environment variables
// or a secrets manager — never from a committed appsettings file.
// Set it with: export Jwt__Key="your-secret" (or Railway environment variables)
//
// CONCEPT: JWT Bearer Authentication
//   1. Client POSTs credentials → AuthController → receives signed JWT.
//   2. Client sends JWT in Authorization: Bearer <token> header.
//   3. This middleware validates the signature and populates HttpContext.User.
//   4. [Authorize] on controllers/actions allows or denies based on that identity.
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
            ClockSkew                = TimeSpan.Zero   // tokens expire exactly at exp claim
        };
    });

builder.Services.AddAuthorization();

// ─── PATTERN: Strategy ─────────────────────────────────────────────────────
// All three strategies are registered for ISortStrategy<Book>.
// DI collects them into IEnumerable<ISortStrategy<Book>> automatically.
// The FIRST registration is the default sort when no name is specified.
builder.Services.AddScoped<ISortStrategy<Book>, TitleSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, AuthorSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, YearSortStrategy>();

// ─── PATTERN: Decorator (three-layer chain) ────────────────────────────────
// The decorator chain at runtime:
//   BookService
//     → CachingBookRepository   (checks cache; avoids DB round-trips)
//       → LoggingBookRepository (logs every operation)
//         → BookRepository      (hits SQLite via EF Core)
//
// Each layer is registered as its concrete type so the next layer can
// resolve it without infinite recursion.
//
// Step 1: innermost concrete repository
builder.Services.AddScoped<BookRepository>();

// Step 2: logging decorator wraps BookRepository
builder.Services.AddScoped<LoggingBookRepository>(sp =>
    new LoggingBookRepository(
        sp.GetRequiredService<BookRepository>(),
        sp.GetRequiredService<ILogger<LoggingBookRepository>>()));

// Step 3: caching decorator wraps LoggingBookRepository.
//         This is what IBookRepository resolves to throughout the app.
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
// Stateless — Singleton lifetime is appropriate.
builder.Services.AddSingleton<ITokenService, TokenService>();

// ─── CONCEPT: BackgroundService ────────────────────────────────────────────
// Logs library statistics on a configurable interval.
// Uses IServiceScopeFactory internally to safely resolve Scoped services
// (IBookService) from within a Singleton-lifetime hosted service.
builder.Services.AddHostedService<LibraryStatsBackgroundService>();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Auto-create the SQLite schema on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CRITICAL ORDER: Authentication must precede Authorization.
// The middleware pipeline is a chain — identity must be established
// (UseAuthentication) before policies can evaluate it (UseAuthorization).
app.UseAuthentication();
app.UseAuthorization();

// CONCEPT: Custom Middleware
// Logs elapsed time for every HTTP request (excluding Blazor SignalR connections).
app.UseRequestTiming();

// MapControllers BEFORE MapFallbackToPage — the fallback catches all
// unmatched routes, so controller routes must be registered first.
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
