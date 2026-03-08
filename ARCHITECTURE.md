# BookLibrary — Architecture & Design Patterns Study Guide

This project is intentionally built to demonstrate as many software engineering
concepts as possible in a real, working application. Use this file as a map.

---

## Project Structure

```
BookLibrary/
├── Models/
│   ├── BaseEntity.cs          ← Inheritance, Abstraction
│   ├── Book.cs                ← Inherits BaseEntity
│   └── ReadingStatus.cs       ← Enum
│
├── Interfaces/
│   ├── IRepository.cs         ← Generic Repository, Dependency Inversion
│   ├── IBookRepository.cs     ← Interface Inheritance, Open/Closed
│   ├── IBookService.cs        ← Service Layer, Dependency Inversion
│   ├── ISortStrategy.cs       ← Strategy Pattern
│   ├── ISpecification.cs      ← Specification Pattern
│   └── IBookFactory.cs        ← Factory Pattern, Dependency Inversion
│
├── Repositories/
│   ├── Repository.cs          ← Generic base (Inheritance, Polymorphism)
│   ├── BookRepository.cs      ← Concrete repo (overrides GetAllAsync)
│   └── LoggingBookRepository  ← Decorator Pattern, Open/Closed
│
├── Services/
│   └── BookService.cs         ← Service Layer, uses Strategy + Specification
│
├── Builders/
│   └── BookBuilder.cs         ← Builder Pattern (fluent API)
│
├── Factories/
│   └── BookFactory.cs         ← Factory Pattern (uses Builder internally)
│
├── Strategies/
│   ├── TitleSortStrategy.cs   ← Strategy concrete implementation
│   ├── AuthorSortStrategy.cs  ← Strategy concrete implementation
│   └── YearSortStrategy.cs    ← Strategy concrete implementation
│
├── Specifications/
│   ├── TitleOrAuthorContainsSpecification.cs  ← Specification
│   ├── StatusSpecification.cs                 ← Specification
│   └── AndSpecification.cs                    ← Composite Specification
│
├── Data/
│   └── AppDbContext.cs        ← EF Core context
│
├── Controllers/
│   ├── BooksController.cs     ← REST API CRUD, JWT-protected
│   └── AuthController.cs      ← POST /api/auth/login → JWT token
│
├── DTOs/
│   ├── BookDto.cs             ← API response shape
│   ├── CreateBookRequest.cs   ← POST body
│   ├── UpdateBookRequest.cs   ← PUT body
│   ├── LoginRequest.cs        ← Auth input
│   └── LoginResponse.cs       ← Auth output (token + expiry)
│
├── Options/
│   ├── JwtOptions.cs          ← JWT config (key, issuer, expiry)
│   ├── CacheOptions.cs        ← Cache duration config
│   └── LibraryStatsOptions.cs ← Background job interval config
│
├── Middleware/
│   ├── RequestTimingMiddleware.cs           ← Logs elapsed ms per request
│   └── RequestTimingMiddlewareExtensions.cs ← app.UseRequestTiming() extension
│
├── BackgroundServices/
│   └── LibraryStatsBackgroundService.cs ← Periodic stats logger (IHostedService)
│
├── Pages/
│   ├── Index.razor            ← Dashboard (stats, currently reading, recently added)
│   ├── Books.razor            ← Injects IBookService + IEnumerable<ISortStrategy>
│   └── BookForm.razor         ← Injects IBookService + IBookFactory
│
├── Shared/
│   ├── MainLayout.razor       ← App shell (sidebar + main content area)
│   ├── NavMenu.razor          ← Sidebar nav + theme toggle (JS Interop)
│   ├── Toast.razor            ← Reusable success/error/info toast component
│   └── ConfirmDialog.razor    ← Reusable modal confirmation dialog
│
├── wwwroot/css/
│   └── site.css               ← Full design system (CSS variables, dark + light theme)
│
├── Pages/_Host.cshtml         ← Inter font, theme JS interop script
│
└── Program.cs                 ← All DI registrations (the composition root)
```

---

## OOP Pillars

### 1. Abstraction
Hide implementation details. Expose only what the caller needs.

- `BaseEntity` — abstract class, cannot be instantiated directly.
- All `I*` interfaces — define contracts without revealing implementation.
- `IBookService` — the UI only knows *what* the service can do, not *how*.

### 2. Encapsulation
Bundle data and behaviour together. Control access to internal state.

- `BookBuilder` — internal `_book` is private. State changes only through the fluent methods. `Build()` is the only exit point.
- `Repository<T>` — `_dbSet` is private. Subclasses get `_context` (protected) but cannot bypass the abstraction.

### 3. Inheritance
Derive new types from existing ones, reusing behaviour without duplication.

- `Book` inherits from `BaseEntity` — gets `Id` and `CreatedAt` for free.
- `BookRepository` inherits from `Repository<Book>` — gets all five CRUD operations, overrides only `GetAllAsync`.
- `IBookRepository` inherits from `IRepository<Book>` — gains all five method signatures.

### 4. Polymorphism
One interface, many implementations. The caller does not know which runs.

- `ISortStrategy<Book>` — `BookService` calls `strategy.Sort(books)`. Whether `TitleSortStrategy`, `AuthorSortStrategy`, or `YearSortStrategy` runs depends on which was selected at runtime.
- `ISpecification<Book>` — `AndSpecification` holds two `ISpecification<Book>` references and calls `IsSatisfiedBy` on each. It does not know the concrete types.
- `IBookRepository` — `BookService` calls `_repository.GetAllAsync()`. The actual call chain at runtime is: `LoggingBookRepository` → `BookRepository` → EF Core.

---

## Design Patterns

### Repository Pattern
**Files:** `IRepository.cs`, `IBookRepository.cs`, `Repository.cs`, `BookRepository.cs`

Separates data access from business logic. The service layer works with
`IBookRepository` and never references Entity Framework, SQLite, or any
storage detail directly. Swapping the database requires changing only the
infrastructure layer.

```
BookService → IBookRepository → BookRepository → EF Core → SQLite
```

### Generic Repository
**File:** `Repository.cs`

Implements CRUD once using `DbContext.Set<T>()`. Any future entity (Author,
Magazine, etc.) gets all five operations by creating a repository that
extends `Repository<NewEntity>`.

### Decorator Pattern
**File:** `LoggingBookRepository.cs`

Wraps `BookRepository` with logging without modifying it.

```
IBookRepository (resolved from DI)
    └── LoggingBookRepository  ← logs, then delegates
            └── BookRepository ← does the actual database work
```

The key: `LoggingBookRepository` implements the same `IBookRepository`
interface as `BookRepository`. The rest of the application never knows
the decorator exists. This is how cross-cutting concerns (logging, caching,
retry, metrics) are added without polluting business code.

**DI wiring in Program.cs:**
```csharp
// Step 1: register concrete type so the factory can resolve it
builder.Services.AddScoped<BookRepository>();

// Step 2: register IBookRepository via a factory that wraps it
builder.Services.AddScoped<IBookRepository>(sp =>
    new LoggingBookRepository(
        sp.GetRequiredService<BookRepository>(),
        sp.GetRequiredService<ILogger<LoggingBookRepository>>()
    ));
```

### Factory Pattern
**Files:** `IBookFactory.cs`, `BookFactory.cs`

Centralises object creation. Callers ask the factory for a `Book`; the
factory decides how to build it. If construction logic changes (new required
field, new default value), only the factory changes — callers are unaffected.

### Builder Pattern
**File:** `BookBuilder.cs`

Constructs a `Book` step by step with a fluent (chainable) API.
`Build()` is the single validation point — an invalid object can never escape.

```csharp
var book = new BookBuilder()
    .WithTitle("Clean Code")
    .WithAuthor("Robert C. Martin")
    .WithGenre("Programming")
    .WithYear(2008)
    .Build();
```

**Factory + Builder composition:**
`BookFactory.Create()` internally uses `BookBuilder`. The factory owns the
*what* (creation policy). The builder owns the *how* (assembly steps).

### Strategy Pattern
**Files:** `ISortStrategy.cs`, `TitleSortStrategy.cs`, `AuthorSortStrategy.cs`, `YearSortStrategy.cs`

Encapsulates sorting algorithms as interchangeable objects.

Without strategy:
```csharp
// Grows forever. Every new sort = more if/else.
if (sort == "Author") books.OrderBy(b => b.Author)
else if (sort == "Year") books.OrderBy(b => b.Year)
```

With strategy:
```csharp
// Always the same. New sort = new class, zero changes here.
strategy.Sort(books)
```

**Registration in Program.cs:**
```csharp
// All three are registered for the same interface.
// DI collects them into IEnumerable<ISortStrategy<Book>> automatically.
builder.Services.AddScoped<ISortStrategy<Book>, TitleSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, AuthorSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, YearSortStrategy>();
```

**End-to-end flow:**
1. `Books.razor` injects `IEnumerable<ISortStrategy<Book>>` and renders a dropdown.
2. User selects "Author". `selectedSort = "Author"`.
3. `SearchAsync("", null, "Author")` is called on `IBookService`.
4. `BookService` finds the strategy where `Name == "Author"`.
5. `AuthorSortStrategy.Sort(books)` runs via polymorphic dispatch.
6. The page renders the sorted list.

### Specification Pattern
**Files:** `ISpecification.cs`, `TitleOrAuthorContainsSpecification.cs`, `StatusSpecification.cs`, `AndSpecification.cs`

Turns a business rule into a named, reusable, composable object.

```csharp
// Each rule is its own class:
var textSpec   = new TitleOrAuthorContainsSpecification("Martin");
var statusSpec = new StatusSpecification(ReadingStatus.Read);

// Compose with AND (Composite pattern):
var combined = new AndSpecification<Book>(textSpec, statusSpec);

// Apply — one call covers both rules:
books.Where(combined.IsSatisfiedBy)
```

**Composite Specification (`AndSpecification`):**
`AndSpecification` itself implements `ISpecification<T>`, so it can be nested:
```csharp
new AndSpecification(ruleA, new AndSpecification(ruleB, ruleC))
```
This is the **Composite** design pattern applied to specifications.

---

## SOLID Principles

| Principle | Where Applied |
|---|---|
| **S** Single Responsibility | `BookRepository` only does data access. `LoggingBookRepository` only logs. `BookBuilder` only builds. Each class does one thing. |
| **O** Open/Closed | `BookRepository` is closed for modification; extended by decoration (`LoggingBookRepository`). New sort orders add a new class, nothing changes. |
| **L** Liskov Substitution | `LoggingBookRepository` can replace `BookRepository` anywhere `IBookRepository` is expected. `AuthorSortStrategy` can replace `TitleSortStrategy` anywhere `ISortStrategy<Book>` is expected. |
| **I** Interface Segregation | `IRepository<T>` has 5 methods. `IBookRepository` inherits and extends only what books need. `ISpecification<T>` has 1 method. |
| **D** Dependency Inversion | `BookService` depends on `IBookRepository`, not `BookRepository`. Blazor pages depend on `IBookService`, not `BookService`. High-level code never depends on low-level details. |

---

## Dependency Injection

Dependency Injection (DI) is the mechanism that makes all the patterns above
work together. Instead of objects creating their own dependencies, they declare
what they need and the DI container provides it.

**Program.cs is the Composition Root** — the one place in the entire
application where all the abstractions are wired to their concrete implementations.

```
IBookRepository  →  LoggingBookRepository( BookRepository )
IBookService     →  BookService( IBookRepository, IEnumerable<ISortStrategy<Book>> )
IBookFactory     →  BookFactory
ISortStrategy<Book> (×3) → TitleSortStrategy, AuthorSortStrategy, YearSortStrategy
```

**Constructor Injection** is used throughout:
```csharp
// BookService declares what it needs. It never calls 'new' on these.
public BookService(
    IBookRepository repository,
    IEnumerable<ISortStrategy<Book>> sortStrategies)
```

**Lifetime choices:**
- `Scoped` — one instance per Blazor circuit (connection). Used for repositories
  and services because they share one `DbContext` per request.
- `Singleton` — one instance for the whole application lifetime. Used for
  `BookFactory` because it is stateless.

---

## How to Add a New Entity (e.g., Author)

This architecture is designed to be extended with minimal effort:

1. Create `Models/Author.cs` extending `BaseEntity`.
2. Create `Interfaces/IAuthorRepository.cs` extending `IRepository<Author>`.
3. Create `Repositories/AuthorRepository.cs` extending `Repository<Author>`.
4. Create `Interfaces/IAuthorService.cs` with author-specific operations.
5. Create `Services/AuthorService.cs` implementing `IAuthorService`.
6. Register in `Program.cs` (the only file that changes for wiring).
7. Add `DbSet<Author>` to `AppDbContext`.

Steps 1–5 involve zero changes to any existing file. Step 6 is additive.
This is the Open/Closed Principle in practice.

---

## UI Layer

### Component Architecture (Blazor)

Each Razor component has a single responsibility, mirroring the same SOLID
principles used in the backend.

| Component | Responsibility |
|---|---|
| `MainLayout.razor` | App shell — renders sidebar + page slot. No business logic. |
| `NavMenu.razor` | Navigation links + theme toggle. Handles JS interop for theme. |
| `Index.razor` | Dashboard. Reads data via `IBookService`, renders stat cards and book grids. |
| `Books.razor` | Book list with search, filter by status, sort by strategy, delete with confirm. |
| `BookForm.razor` | Add/edit form. Uses `IBookFactory` to create, `IBookService` to persist. |
| `Toast.razor` | Stateless presentational component. Receives `IsVisible`, `Message`, `Type`. |
| `ConfirmDialog.razor` | Modal dialog. Raises `OnConfirm` / `OnCancel` event callbacks to parent. |

### Component Communication Patterns

**Parent → Child (Parameters):**
```razor
<Toast IsVisible="@showToast" Message="@toastMessage" Type="success" />
<ConfirmDialog IsVisible="@showDialog" OnConfirm="DeleteConfirmed" OnCancel="CancelDelete" />
```

**Child → Parent (EventCallback):**
```csharp
// ConfirmDialog.razor
[Parameter] public EventCallback OnConfirm { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```
The dialog does not know what happens on confirm/cancel — the parent decides.
This is the same Dependency Inversion principle applied to the UI layer.

**Cross-page messaging (Query String):**
After saving a book, `BookForm` navigates to `/books?toast=Book saved!`.
`Books.razor` reads the query string on init and shows the toast.
No shared state service is needed — the URL carries the message.

### CSS Design System

All visual tokens are defined as CSS custom properties in `site.css`:

```css
:root {
    --bg-base:     #0D0D14;   /* page background  */
    --bg-surface:  #13131F;   /* card background  */
    --accent:      #7C3AED;   /* primary purple   */
    --text:        #F1F0FF;   /* primary text     */
    --border:      rgba(255,255,255,0.07);
    --gradient-text: linear-gradient(135deg, #A78BFA, #7C3AED);
}
```

Every component uses these variables — never hard-coded colours. This means
the entire theme can be changed by overriding the variables in one place.

### Dark / Light Theme

**How it works:**
1. `_Host.cshtml` includes an inline script that runs before the page renders.
   It reads `localStorage` and sets `data-theme="light"` on `<html>` if saved.
   This prevents a flash of the wrong theme on page load.

2. `site.css` defines a `[data-theme="light"]` block that overrides every
   colour variable:
   ```css
   [data-theme="light"] {
       --bg-base:    #F4F4F8;
       --bg-surface: #FFFFFF;
       --text:       #1A1A2E;
       /* ... all tokens overridden */
   }
   ```

3. `NavMenu.razor` injects `IJSRuntime` and calls `themeInterop.setTheme()`
   to toggle the attribute and persist the preference.

**JS Interop pattern:**
```csharp
// Read saved preference after first render (not during prerender)
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var saved = await JS.InvokeAsync<string>("themeInterop.getTheme");
        isDarkTheme = saved != "light";
        StateHasChanged();
    }
}

// Toggle and persist
private async Task ToggleTheme()
{
    isDarkTheme = !isDarkTheme;
    await JS.InvokeVoidAsync("themeInterop.setTheme", isDarkTheme ? "dark" : "light");
}
```

`OnAfterRenderAsync` is used (not `OnInitializedAsync`) because JS interop
is not available during server-side pre-rendering.

### Toast Notification Pattern

`Toast.razor` is a pure presentational (dumb) component — it only renders.
The parent page owns the state and the timer:

```csharp
// Books.razor
private async Task ShowToastThenHide(string msg, string type = "success")
{
    toastMessage = msg;
    toastType    = type;
    showToast    = true;
    StateHasChanged();
    await Task.Delay(2800);
    showToast = false;
    StateHasChanged();
}
```

This pattern keeps `Toast.razor` reusable — it works identically on any page.

---

## REST API Layer

### Controllers

The project exposes a full REST API alongside the Blazor UI. Both share the
same `IBookService` — zero duplication of business logic.

| Controller | Route | Description |
|---|---|---|
| `AuthController` | `POST /api/auth/login` | Returns a JWT token |
| `BooksController` | `GET/POST/PUT/DELETE /api/books` | Full CRUD, JWT-protected |

### DTOs (Data Transfer Objects)

Domain models (`Book`) are never returned directly from the API. They are
mapped to DTOs which form a stable, independent API contract.

```
Book (domain) → BookDto (API response)
CreateBookRequest (API input) → Book (via IBookFactory)
```

**Files:** `DTOs/BookDto.cs`, `CreateBookRequest.cs`, `UpdateBookRequest.cs`,
`LoginRequest.cs`, `LoginResponse.cs`

### JWT Authentication

The API layer uses stateless JWT Bearer authentication. The Blazor UI is
unaffected — it uses its own session model.

**Flow:**
```
1. POST /api/auth/login  { username, password }
2. Server validates → TokenService generates signed JWT
3. Client sends: Authorization: Bearer <token>
4. JWT middleware validates signature → populates HttpContext.User
5. [Authorize] on BooksController allows/denies access
```

**Files:** `Options/JwtOptions.cs`, `Services/TokenService.cs`,
`Controllers/AuthController.cs`

**DI registration in Program.cs:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* TokenValidationParameters */ });
builder.Services.AddSingleton<ITokenService, TokenService>();
```

**Pipeline order (critical):**
```csharp
app.UseAuthentication();   // establishes identity from JWT
app.UseAuthorization();    // evaluates [Authorize] using that identity
app.MapControllers();      // BEFORE MapFallbackToPage — fallback catches everything
```

---

## Options Pattern

Configuration is never read as raw strings. Each feature has a strongly-typed
options class bound from `appsettings.json`.

| Class | Section | Used by |
|---|---|---|
| `JwtOptions` | `"Jwt"` | `TokenService`, JWT middleware |
| `CacheOptions` | `"Cache"` | `CachingBookRepository` |
| `LibraryStatsOptions` | `"LibraryStats"` | `LibraryStatsBackgroundService` |

**Registration:**
```csharp
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));
```

**Consumption:**
```csharp
public CachingBookRepository(IOptions<CacheOptions> options, ...)
    => _options = options.Value;
```

This pattern makes configuration testable (pass `Options.Create(new CacheOptions {...})`)
and refactor-friendly (rename the property, not the string).

---

## Caching (Extended Decorator Chain)

`CachingBookRepository` is a third Decorator that wraps `LoggingBookRepository`.
The full chain at runtime:

```
BookService
  → CachingBookRepository   (cache-aside: serve from cache or fetch+store)
    → LoggingBookRepository (logs every call)
      → BookRepository      (SQLite via EF Core)
```

**Strategy: Cache-aside / Write-invalidate**
- Reads: serve from `IMemoryCache` if present; otherwise fetch and cache.
- Writes: always delegate to inner repo, then remove stale cache entries.

**DI wiring (three-step concrete registration):**
```csharp
builder.Services.AddScoped<BookRepository>();           // step 1 — innermost
builder.Services.AddScoped<LoggingBookRepository>(...); // step 2 — middle
builder.Services.AddScoped<IBookRepository>(...         // step 3 — outermost
    new CachingBookRepository(
        sp.GetRequiredService<LoggingBookRepository>(), ...));
```

Each decorator is registered as its **concrete type** so the next layer can
resolve it. Only the outermost is registered as `IBookRepository`.

---

## Middleware

**File:** `Middleware/RequestTimingMiddleware.cs`

Custom middleware that measures and logs elapsed time for every HTTP request.

```
Request  → [RequestTimingMiddleware] → [Auth] → [Controller/Blazor]
Response ← [RequestTimingMiddleware] ← logs elapsed + status code
```

**Middleware pipeline concepts:**
- `try/finally` ensures timing is always logged, even on exceptions.
- `ILogger<T>` injected via **constructor** (singleton-safe).
- Scoped services go in `InvokeAsync` parameters (per-request).
- Blazor SignalR connections (`/_blazor`) are skipped — they are long-lived
  websockets and their elapsed time would be meaningless.

**Registration via extension method (conventional pattern):**
```csharp
// Middleware/RequestTimingMiddlewareExtensions.cs
public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
    => app.UseMiddleware<RequestTimingMiddleware>();

// Program.cs
app.UseRequestTiming();
```

---

## Background Service

**File:** `BackgroundServices/LibraryStatsBackgroundService.cs`

A hosted background task that logs library statistics on a configurable interval.

```csharp
public class LibraryStatsBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
            await LogStatsAsync(stoppingToken);
        }
    }
}
```

**Critical: Scoped service in a Singleton**

`AddHostedService<T>` registers the service as a **Singleton**. `IBookService`
is registered as **Scoped**. You cannot inject a Scoped service into a Singleton
constructor — it would capture a stale scope forever, leaking `DbContext`.

**Solution: `IServiceScopeFactory`**
```csharp
// Inject the factory (Singleton-safe), not the service (Scoped)
public LibraryStatsBackgroundService(IServiceScopeFactory scopeFactory, ...)

// Create a fresh scope per tick — disposes cleanly after use
await using var scope = _scopeFactory.CreateAsyncScope();
var bookService = scope.ServiceProvider.GetRequiredService<IBookService>();
```

This is a fundamental pattern for any background work that needs database access.

---

## Unit Testing

**Project:** `BookLibrary.Tests/` — xUnit + Moq

**42 tests covering:**

| Test file | What is tested |
|---|---|
| `BookBuilderTests.cs` | Fluent builder, validation, all status values |
| `BookServiceTests.cs` | Search filtering, strategy selection, CRUD delegation |
| `TitleOrAuthorContainsSpecificationTests.cs` | Case-insensitive text matching |
| `StatusSpecificationTests.cs` | Status filter matching |
| `AndSpecificationTests.cs` | Composition, nesting (Composite pattern) |
| `SortStrategyTests.cs` | All three strategies + null Year handling |
| `CachingBookRepositoryTests.cs` | Cache HIT, MISS, write invalidation |

**Mocking with Moq:**
```csharp
// Arrange — replace IBookRepository with a fake
var repoMock = new Mock<IBookRepository>();
repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(books);

// Assert — verify the method was called exactly once
repoMock.Verify(r => r.GetAllAsync(), Times.Once);
```

**Testing the Caching Decorator:**
`IMemoryCache` is complex to mock — a real `MemoryCache` is used instead.
`NullLogger<T>.Instance` provides a no-op logger with zero configuration.

**Run tests:**
```bash
dotnet test
```

---

## Key Takeaways

- **Interfaces decouple layers.** The UI, service, and data layers only know about each other through interfaces. Any layer can be replaced without touching the others.
- **Patterns solve recurring problems.** Strategy eliminates if/else chains. Specification makes filters composable and reusable. Decorator adds behaviour without inheritance.
- **DI is the glue.** All the patterns are made practical by dependency injection — the container wires everything together at startup, and every class just declares its dependencies.
- **Inheritance gives you reuse; interfaces give you flexibility.** Use inheritance when classes genuinely share behaviour (Repository base). Use interfaces when you want interchangeability (ISortStrategy, ISpecification).
- **UI components follow the same SOLID rules.** `Toast` and `ConfirmDialog` are small, single-purpose, and reusable. Parent pages own state; child components only render and raise events.
- **CSS variables are the design system.** One set of tokens drives the entire visual appearance. Switching themes requires overriding variables in one selector — not touching any component.
- **JS interop only after render.** Browser APIs (localStorage, DOM attributes) are only available after Blazor has hydrated. Always use `OnAfterRenderAsync` with `firstRender` guard for JS calls.
- **Never inject Scoped into Singleton.** Background services are Singletons. Use `IServiceScopeFactory` to create a fresh scope per tick when you need `DbContext` or other scoped services.
- **Test behaviour, not implementation.** Mock `IBookRepository` (not `BookRepository`) to test `BookService`. The test never knows about EF Core — it only sees the interface.
- **Options Pattern over raw IConfiguration.** Bind config to strongly-typed classes so configuration is testable (`Options.Create(new JwtOptions {...})`), refactor-safe, and self-documenting.
- **Middleware pipeline order is critical.** `UseAuthentication` must precede `UseAuthorization`. `MapControllers` must precede `MapFallbackToPage`. Getting this wrong is one of the most common ASP.NET Core mistakes.
