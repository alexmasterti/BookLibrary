# BookLibrary — Claude Code Instructions

## Project Overview

BookLibrary is a Blazor Server (.NET 7) application that serves as a comprehensive
.NET portfolio/study sample. It intentionally demonstrates as many real-world
patterns as possible in a single working codebase.

**Live deployment:** https://booklibrary-production-64d2.up.railway.app
**GitHub:** https://github.com/alexmasterti/BookLibrary
**Architecture reference:** See `ARCHITECTURE.md` for full pattern documentation.

---

## Tech Stack

- **Framework:** Blazor Server, .NET 7, C# 11
- **Database:** SQLite via Entity Framework Core 7
- **Authentication:** JWT Bearer (API layer only)
- **Caching:** `IMemoryCache` (in-process, singleton)
- **API Docs:** Swagger UI via Swashbuckle (`/swagger` — Development only)
- **Testing:** xUnit + Moq (`BookLibrary.Tests/` project)
- **Deployment:** Railway (auto-deploys on push to `main`)

---

## Running the App

### F5 in VS Code
Press **F5** — uses `.vscode/launch.json` which runs on `http://localhost:5074`.

### Terminal
```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5074 dotnet run
```

### If port 5074 is already in use
```bash
lsof -ti :5074 | xargs kill -9
```

### Common: stale build artifacts causing BadImageFormatException
```bash
rm -rf bin obj && dotnet build
```

**HTTPS dev certs do not work on macOS.** Always use HTTP via `ASPNETCORE_URLS`.
The `launch.json` already has this set. Do not attempt `dotnet dev-certs https --trust`.

---

## Running Tests

```bash
cd /Users/alexcs/Projects/BookLibrary.Tests
dotnet test
```

42 tests total. Test project is at `/Users/alexcs/Projects/BookLibrary.Tests/`.

---

## API Testing

Swagger UI is available at `http://localhost:5074/swagger` when running in Development.

**Login credentials:**
- Username: `admin`
- Password: `BookShelf2024!`

**Flow:** POST `/api/auth/login` → copy `token` → click Authorize in Swagger → paste token.

---

## Project Structure

```
BookLibrary/               ← main app (Blazor Server)
BookLibrary.Tests/         ← xUnit test project (separate directory)
BookLibrary.sln            ← solution file linking both projects
```

Key folders in the main app:

| Folder | Purpose |
|---|---|
| `Controllers/` | REST API — `AuthController`, `BooksController` |
| `DTOs/` | API request/response contracts (never expose domain models directly) |
| `Services/` | Business logic — `BookService`, `TokenService` |
| `Repositories/` | Data access — `BookRepository`, `LoggingBookRepository`, `CachingBookRepository` |
| `Interfaces/` | All abstractions — `IBookRepository`, `IBookService`, `ISortStrategy`, etc. |
| `Models/` | Domain models — `Book`, `BaseEntity`, `ReadingStatus` |
| `Options/` | Strongly-typed config — `JwtOptions`, `CacheOptions`, `LibraryStatsOptions` |
| `Middleware/` | `RequestTimingMiddleware` — logs elapsed ms per request |
| `BackgroundServices/` | `LibraryStatsBackgroundService` — periodic stats logger |
| `Strategies/` | Sort strategies — Title, Author, Year |
| `Specifications/` | Filter specs — text contains, status, AND composite |
| `Builders/` | `BookBuilder` — fluent builder pattern |
| `Factories/` | `BookFactory` — creation policy using BookBuilder |
| `Pages/` | Blazor pages — `Index`, `Books`, `BookForm` |
| `Shared/` | Blazor components — `NavMenu`, `Toast`, `ConfirmDialog` |

---

## Key Architecture Decisions

### Decorator Chain (Repository Layer)
The full chain at runtime — do not break this ordering:
```
CachingBookRepository → LoggingBookRepository → BookRepository → EF Core → SQLite
```
Each is registered as its concrete type in `Program.cs`; only `CachingBookRepository`
is the `IBookRepository` binding. Changing this requires updating the three-step
registration block in `Program.cs`.

### Dependency Injection Lifetimes
- `Scoped` — repositories, services (share one `DbContext` per circuit)
- `Singleton` — `BookFactory`, `ITokenService`, `IMemoryCache`
- `Hosted` — `LibraryStatsBackgroundService` (Singleton — use `IServiceScopeFactory`
  to resolve scoped services inside it, never constructor-inject `IBookService` directly)

### Middleware Pipeline Order (critical — do not reorder)
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTiming();   // custom middleware
app.MapControllers();     // BEFORE MapFallbackToPage
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
```

### Swagger (Development only)
`app.UseSwagger()` and `app.UseSwaggerUI()` are inside `if (app.Environment.IsDevelopment())`.
Swagger is NOT available on Railway (Production). This is intentional.

### Theme Toggle
- Default theme is **dark**.
- Theme is stored in `localStorage` and applied via `data-theme="light"` on `<html>`.
- The inline script in `_Host.cshtml` applies the saved theme before first render
  to prevent flash.
- JS interop lives in `NavMenu.razor` — only called in `OnAfterRenderAsync` (never
  `OnInitializedAsync`, which runs during server-side prerender where JS is unavailable).

---

## Configuration (`appsettings.json`)

```json
{
  "Cache":        { "BooksCacheDurationSeconds": 60 },
  "LibraryStats": { "IntervalSeconds": 300 },
  "Jwt":          { "Key": "BookShelf-Dev-Secret-Key-32chars!!", "Issuer": "BookLibrary", "Audience": "BookLibraryApiClients", "ExpiryMinutes": 60 },
  "ApiCredentials": { "Username": "admin", "Password": "BookShelf2024!" }
}
```

**Production (Railway):** Set `Jwt__Key` as an environment variable — never commit a real secret.

---

## Database

SQLite file: `books.db` (created automatically on first run via `EnsureCreated()`).
View it with the **SQLite Viewer** VS Code extension or `sqlite3 books.db` in terminal.
The `.gitignore` excludes `*.db`, `*.db-shm`, `*.db-wal`.

---

## Deployment

Railway auto-deploys on every push to `main`:
```bash
git push origin main
```

No manual steps needed. Railway reads the `.csproj` and runs `dotnet run`.

---

## Testing Notes

- Test project uses C# 11 (`.NET 7`) — use `new List<Book> {}` syntax, not `[...]`
  collection expressions (those require C# 12).
- `IMemoryCache` is not mocked — a real `MemoryCache` is instantiated in tests.
- `IOptions<T>` in tests: use `Microsoft.Extensions.Options.Options.Create(new T {...})`.
  The `BookLibrary.Options` namespace can shadow the `Options` class — use fully qualified
  name if ambiguous.
- `NullLogger<T>.Instance` for any logger dependencies in unit tests.
