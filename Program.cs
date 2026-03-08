using BookLibrary.Data;
using BookLibrary.Factories;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Repositories;
using BookLibrary.Services;
using BookLibrary.Strategies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ─── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=books.db"));

// ─── PATTERN: Strategy ─────────────────────────────────────────────────────
// Register each concrete sort strategy against the shared ISortStrategy<Book>
// interface. ASP.NET Core DI automatically collects all registrations for the
// same interface into IEnumerable<ISortStrategy<Book>> when it is requested.
// BookService and Books.razor receive the full list without knowing the types.
//
// IMPORTANT: The FIRST registered strategy is the default sort order.
// Change the order here to change the application's default sorting.
builder.Services.AddScoped<ISortStrategy<Book>, TitleSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, AuthorSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, YearSortStrategy>();

// ─── PATTERN: Decorator ────────────────────────────────────────────────────
// Microsoft.Extensions.DependencyInjection does not support automatic
// decorator chaining, so we wire it manually in two steps:
//
// Step 1: Register BookRepository as its CONCRETE type (not as IBookRepository).
//         This allows the factory below to resolve it without recursion.
builder.Services.AddScoped<BookRepository>();

// Step 2: Register IBookRepository via a factory lambda that wraps
//         BookRepository inside LoggingBookRepository.
//         The rest of the application only ever sees IBookRepository —
//         it is completely unaware that logging is happening underneath.
//
// PRINCIPLE: Open/Closed (SOLID — 'O')
//   BookRepository is closed for modification. We added logging by
//   decorating it, not by changing it.
builder.Services.AddScoped<IBookRepository>(sp =>
    new LoggingBookRepository(
        sp.GetRequiredService<BookRepository>(),           // the real repository
        sp.GetRequiredService<ILogger<LoggingBookRepository>>() // logging from DI
    ));

// ─── PRINCIPLE: Dependency Inversion (SOLID — 'D') ────────────────────────
// Register IBookService -> BookService.
// Blazor pages inject IBookService. They never reference BookService directly,
// which means the implementation can be swapped (e.g., for a mock in tests)
// without touching a single page.
builder.Services.AddScoped<IBookService, BookService>();

// ─── PATTERN: Factory ──────────────────────────────────────────────────────
// BookFactory is stateless, so Singleton lifetime is appropriate —
// one instance is shared for the entire application lifetime.
builder.Services.AddSingleton<IBookFactory, BookFactory>();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Auto-create the database schema on startup (no migrations needed for this app).
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
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
