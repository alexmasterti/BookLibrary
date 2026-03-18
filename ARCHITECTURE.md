# BookLibrary — Architecture & Design Guide

> **Who this is for:** Junior developers joining the project, or anyone who wants to understand
> not just *what* the code does, but *why* it was written this way.
>
> This is not documentation for documentation's sake. Every decision in this codebase has a reason.
> This file explains those reasons.

---

## Table of Contents

1. [What Is This App?](#what-is-this-app)
2. [Project Structure](#project-structure)
3. [How the Layers Talk to Each Other](#how-the-layers-talk-to-each-other)
4. [OOP Pillars](#oop-pillars)
5. [Design Patterns](#design-patterns)
6. [SOLID Principles](#solid-principles)
7. [Dependency Injection](#dependency-injection)
8. [REST API & JWT Authentication](#rest-api--jwt-authentication)
9. [FluentValidation](#fluentvalidation)
10. [Pagination](#pagination)
11. [Caching — The Decorator Chain](#caching--the-decorator-chain)
12. [Health Checks](#health-checks)
13. [Global Exception Handling & ProblemDetails](#global-exception-handling--problemdetails)
14. [Rate Limiting](#rate-limiting)
15. [AI Book Recommendations](#ai-book-recommendations)
16. [Options Pattern](#options-pattern)
17. [Middleware](#middleware)
18. [Background Service](#background-service)
19. [UI Layer — Blazor](#ui-layer--blazor)
20. [Unit Testing](#unit-testing)
21. [Middleware Pipeline Order — Why It Matters](#middleware-pipeline-order--why-it-matters)
22. [How to Add a New Feature](#how-to-add-a-new-feature)
23. [Key Takeaways](#key-takeaways)

---

## What Is This App?

BookLibrary is a personal reading tracker. You can:
- Add books and mark them as *Want to Read*, *Currently Reading*, or *Read*
- Search, filter, and sort your list
- Get AI-powered book recommendations based on your reading history
- Interact via a web UI (Blazor) or a REST API

**Under the hood, it is intentionally over-engineered** — not because a book tracker needs this
complexity, but because it is a learning project designed to demonstrate as many real-world
software engineering patterns as possible in one working application.

Think of it as a living reference you can read, run, and modify.

---

## Project Structure

```
BookLibrary/
│
├── Models/                         ← Domain objects (what the app is about)
│   ├── BaseEntity.cs               ← Abstract base: Id + CreatedAt for every entity
│   ├── Book.cs                     ← The main domain model
│   └── ReadingStatus.cs            ← Enum: WantToRead | CurrentlyReading | Read
│
├── Interfaces/                     ← Contracts (what things CAN do, not how)
│   ├── IRepository.cs              ← Generic CRUD contract for any entity
│   ├── IBookRepository.cs          ← Book-specific repository contract
│   ├── IBookService.cs             ← All book business operations
│   ├── IBookFactory.cs             ← Creates Book objects
│   ├── IBookRecommendationService  ← AI recommendation contract
│   ├── ISortStrategy.cs            ← Pluggable sort algorithm contract
│   └── ISpecification.cs           ← Filter rule contract
│
├── Repositories/                   ← Data access layer
│   ├── Repository.cs               ← Generic base (CRUD via EF Core)
│   ├── BookRepository.cs           ← Concrete book data access
│   ├── LoggingBookRepository.cs    ← Decorator: adds logging to any repo
│   └── CachingBookRepository.cs    ← Decorator: adds caching to any repo
│
├── Services/                       ← Business logic layer
│   ├── BookService.cs              ← Core book operations + pagination
│   ├── BookRecommendationService.cs← Calls Anthropic Claude API
│   └── TokenService.cs             ← Generates and validates JWT tokens
│
├── Controllers/                    ← REST API endpoints
│   ├── BooksController.cs          ← CRUD + search + pagination endpoints
│   ├── AuthController.cs           ← POST /api/auth/login
│   └── RecommendationsController.cs← GET /api/recommendations
│
├── DTOs/                           ← API request/response shapes
│   ├── BookDto.cs                  ← What the API returns for a book
│   ├── CreateBookRequest.cs        ← What POST /api/books expects
│   ├── UpdateBookRequest.cs        ← What PUT /api/books/{id} expects
│   ├── PagedBooksRequest.cs        ← Query params for paginated endpoint
│   ├── PaginatedResult.cs          ← Generic paginated response wrapper
│   ├── BookRecommendationResult.cs ← AI recommendation response
│   ├── RecommendedBook.cs          ← A single AI recommendation
│   ├── LoginRequest.cs             ← Auth input
│   └── LoginResponse.cs            ← Auth output (token + expiry)
│
├── Validators/                     ← FluentValidation rules
│   ├── CreateBookValidator.cs      ← Validates POST body
│   └── UpdateBookValidator.cs      ← Validates PUT body
│
├── Specifications/                 ← Composable filter rules
│   ├── TitleOrAuthorContainsSpecification.cs
│   ├── StatusSpecification.cs
│   └── AndSpecification.cs         ← Combines two specs with AND
│
├── Strategies/                     ← Pluggable sort algorithms
│   ├── TitleSortStrategy.cs
│   ├── AuthorSortStrategy.cs
│   └── YearSortStrategy.cs
│
├── Factories/                      ← Centralized object creation
│   └── BookFactory.cs
│
├── Builders/                       ← Step-by-step object assembly
│   └── BookBuilder.cs              ← Fluent builder for Book
│
├── Options/                        ← Strongly-typed config classes
│   ├── JwtOptions.cs
│   ├── CacheOptions.cs
│   ├── LibraryStatsOptions.cs
│   └── AnthropicOptions.cs         ← AI API key + model name
│
├── Middleware/                     ← Custom HTTP pipeline components
│   ├── RequestTimingMiddleware.cs  ← Logs ms per request
│   └── GlobalExceptionMiddleware.cs← Catches all errors → RFC 7807 JSON
│
├── BackgroundServices/
│   └── LibraryStatsBackgroundService.cs ← Periodic stats logger
│
├── Data/
│   └── AppDbContext.cs             ← EF Core database context
│
├── Pages/                          ← Blazor UI pages
│   ├── Index.razor                 ← Dashboard
│   ├── Books.razor                 ← Book list + search + filter + sort
│   ├── BookForm.razor              ← Add / edit form
│   └── Recommendations.razor       ← AI recommendations UI
│
├── Shared/                         ← Reusable Blazor components
│   ├── MainLayout.razor            ← App shell
│   ├── NavMenu.razor               ← Sidebar navigation
│   ├── Toast.razor                 ← Success/error notification
│   └── ConfirmDialog.razor         ← Modal confirmation dialog
│
├── wwwroot/css/site.css            ← CSS design system (variables, dark/light theme)
├── appsettings.json                ← Configuration (JWT, Cache, Anthropic, etc.)
└── Program.cs                      ← Composition root — ALL wiring happens here
```

---

## How the Layers Talk to Each Other

This is the most important diagram in the entire file. Read it once and the rest will click.

```
┌─────────────────────────────────────────────────────────────┐
│                     USER / BROWSER                          │
└────────────┬──────────────────────────┬────────────────────-┘
             │ Blazor (WebSocket)        │ REST API (HTTP)
             ▼                           ▼
┌─────────────────────┐      ┌─────────────────────────┐
│   Pages/*.razor     │      │  Controllers/*.cs        │
│  (Blazor UI layer)  │      │  (API layer)             │
└────────┬────────────┘      └──────────┬───────────────┘
         │                              │
         │  Both inject IBookService    │
         ▼                              ▼
┌────────────────────────────────────────────────────────┐
│                   BookService                          │
│  (Business logic: search, filter, sort, paginate)     │
│  Uses: Strategy Pattern + Specification Pattern        │
└────────────────────────┬───────────────────────────────┘
                         │ Calls IBookRepository
                         ▼
┌────────────────────────────────────────────────────────┐
│  CachingBookRepository  (Decorator — outermost)        │
│  → LoggingBookRepository (Decorator — middle)          │
│    → BookRepository (concrete — innermost)             │
│      → AppDbContext (EF Core)                          │
│        → SQLite file (books.db)                        │
└────────────────────────────────────────────────────────┘
```

**The golden rule:** each layer only knows about the layer directly below it,
and it only knows it through an **interface** — never a concrete class.

This means you can swap SQLite for SQL Server, swap the cache implementation,
or replace the UI framework, and the business logic code does not change.

---

## OOP Pillars

These are the four foundational concepts of object-oriented programming.
This project demonstrates all four in real, working code.

### 1. Abstraction — Hide the Details

Show callers *what* they can do, not *how* it works internally.

- `IBookService` — Blazor pages call `GetAllBooksAsync()`. They have no idea if the
  data comes from SQLite, an API, or a text file. That is abstraction.
- All `I*` interfaces are abstractions — they are contracts, not implementations.
- `BaseEntity` — abstract class that cannot be instantiated. It forces subclasses
  to inherit `Id` and `CreatedAt` without being usable on their own.

### 2. Encapsulation — Protect the State

Bundle data and the code that operates on it into one unit. Prevent external
code from putting an object into an invalid state.

- `BookBuilder` — the `_book` field is `private`. You cannot set `Title` directly
  on a half-built book. You must go through `WithTitle()`. `Build()` validates
  everything before returning the object. A bad book cannot escape.
- `Repository<T>` — `_dbSet` is `private`. Subclasses can use `_context` (protected)
  but cannot reach into the raw DbSet and bypass the repository's methods.

### 3. Inheritance — Reuse Without Duplication

Build new types on top of existing ones, inheriting their behaviour.

- `Book` inherits from `BaseEntity` — it automatically has `Id` and `CreatedAt`
  without declaring them. Every future entity (Author, Review, etc.) gets the same.
- `BookRepository` inherits from `Repository<Book>` — it gets `GetByIdAsync`,
  `AddAsync`, `UpdateAsync`, `DeleteAsync` for free, and only overrides `GetAllAsync`
  to add eager loading.
- `IBookRepository` inherits from `IRepository<Book>` — it gets all five method
  signatures and can add book-specific ones.

### 4. Polymorphism — One Interface, Many Behaviours

Write code that works with any implementation of an interface, without caring
which specific class runs at runtime.

- `ISortStrategy<Book>` — `BookService.SearchAsync()` calls `strategy.Sort(books)`.
  The same line of code can run `TitleSortStrategy`, `AuthorSortStrategy`, or
  `YearSortStrategy` — decided at runtime, not at compile time.
- `IBookRepository` — `BookService` calls `_repository.GetAllAsync()`. At runtime,
  this actually runs through the full decorator chain: Caching → Logging → EF Core.
  `BookService` is completely unaware of this.

---

## Design Patterns

Design patterns are proven solutions to recurring problems. They have names
so teams can communicate clearly: "use a decorator here" means something specific.

### Repository Pattern

**Files:** `IRepository.cs`, `IBookRepository.cs`, `Repository.cs`, `BookRepository.cs`

**Problem:** Business logic and database code get tangled together. Testing becomes
impossible without a real database. Swapping databases means rewriting half the app.

**Solution:** All database access goes through a repository. The service layer only
talks to `IBookRepository`. It never touches `DbContext`, SQL, or EF Core directly.

```
BookService → IBookRepository → BookRepository → EF Core → SQLite
```

**Real benefit:** In unit tests, you replace `IBookRepository` with a fake (mock).
Tests run in milliseconds with no database. `BookService` never knows the difference.

### Generic Repository

**File:** `Repository.cs`

**Problem:** Every entity needs the same five operations (GetAll, GetById, Add, Update,
Delete). Copy-pasting them for every entity is wasteful and error-prone.

**Solution:** Implement CRUD once using `DbContext.Set<T>()`. Any entity gets all five
operations by creating one class that extends `Repository<ThatEntity>`.

```csharp
// Adding a new entity? Just do this:
public class AuthorRepository : Repository<Author>
{
    public AuthorRepository(AppDbContext context) : base(context) { }
    // Done. All 5 CRUD operations inherited for free.
}
```

### Decorator Pattern

**Files:** `LoggingBookRepository.cs`, `CachingBookRepository.cs`

**Problem:** You want to add logging and caching to the repository, but you
do not want to put that code inside `BookRepository`. That would violate the
Single Responsibility Principle — `BookRepository` should only do data access.

**Solution:** Wrap the repository in another class that implements the same interface,
does its cross-cutting work (log, cache), then delegates to the inner repo.

```
IBookRepository (what the service sees)
    └── CachingBookRepository  ← checks cache; calls inner if miss
          └── LoggingBookRepository  ← logs every call; calls inner
                └── BookRepository  ← actual EF Core database work
```

The key insight: every layer implements `IBookRepository`. `BookService` calls
`_repository.GetAllAsync()` — it has no idea three classes run before EF Core does.

**Adding a new cross-cutting concern** (e.g., retry logic, metrics) means creating
one new class, not modifying any existing code. This is the Open/Closed Principle.

### Factory Pattern

**Files:** `IBookFactory.cs`, `BookFactory.cs`

**Problem:** Creating a `Book` requires multiple steps and validation. If you do this
in 10 different places, a change to the Book constructor breaks 10 places.

**Solution:** One factory owns all creation logic. Callers ask `_factory.Create(...)`.
The factory decides how to build the object. Change the factory, nothing else changes.

```csharp
// The controller doesn't know how Book is constructed:
var book = _bookFactory.Create(request.Title, request.Author, request.Genre, request.Year, status);
```

### Builder Pattern

**File:** `BookBuilder.cs`

**Problem:** Constructors with many parameters are hard to read and easy to get wrong.
`new Book("", null, "Sci-Fi", 1984, status)` — which argument is which?

**Solution:** A fluent builder lets you set properties by name, in any order,
with a single `Build()` call at the end that validates everything.

```csharp
var book = new BookBuilder()
    .WithTitle("Clean Code")
    .WithAuthor("Robert C. Martin")
    .WithGenre("Programming")
    .WithYear(2008)
    .Build();
```

`Build()` throws if Title or Author is missing. An invalid book can never be created.

**Factory + Builder working together:**
`BookFactory.Create()` calls `BookBuilder` internally. The factory decides the
policy ("what to build"). The builder decides the steps ("how to assemble it").

### Strategy Pattern

**Files:** `ISortStrategy.cs`, `TitleSortStrategy.cs`, `AuthorSortStrategy.cs`, `YearSortStrategy.cs`

**Problem:** Sorting logic starts small, then grows into a mess of if/else:

```csharp
// This grows forever. Every new sort option = more branches.
if (sort == "Title")  return books.OrderBy(b => b.Title);
if (sort == "Author") return books.OrderBy(b => b.Author);
if (sort == "Year")   return books.OrderBy(b => b.Year);
```

**Solution:** Each sort algorithm is its own class implementing `ISortStrategy<Book>`.
The service just calls `strategy.Sort(books)`. Adding a new sort means adding
one new class — zero changes to `BookService`.

```csharp
// BookService — never changes when new sorts are added:
var strategy = _sortStrategies.FirstOrDefault(s => s.Name == sortStrategyName)
               ?? _sortStrategies.First();
return strategy.Sort(books).ToList();
```

**How DI makes it work:**
```csharp
// All three registered for the same interface.
// ASP.NET Core automatically collects them into IEnumerable<ISortStrategy<Book>>.
builder.Services.AddScoped<ISortStrategy<Book>, TitleSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, AuthorSortStrategy>();
builder.Services.AddScoped<ISortStrategy<Book>, YearSortStrategy>();
```

### Specification Pattern

**Files:** `ISpecification.cs`, `TitleOrAuthorContainsSpecification.cs`, `StatusSpecification.cs`, `AndSpecification.cs`

**Problem:** Filtering logic becomes complex and scattered. You end up with
long LINQ chains mixed into business logic that are hard to name, reuse, or test.

**Solution:** Each filter rule is a named class with one method: `IsSatisfiedBy(item)`.
Rules can be combined using `AndSpecification`.

```csharp
// Each rule is readable, named, and independently testable:
var textRule   = new TitleOrAuthorContainsSpecification("Tolkien");
var statusRule = new StatusSpecification(ReadingStatus.Read);

// Combine them — AndSpecification is itself a specification:
var both = new AndSpecification<Book>(textRule, statusRule);

// Apply — one clean call:
var filtered = books.Where(both.IsSatisfiedBy).ToList();
```

`AndSpecification` implements `ISpecification<T>` — so it can be nested
inside another `AndSpecification`. This is the **Composite Pattern**.

---

## SOLID Principles

SOLID is an acronym for five principles that make code easier to maintain,
extend, and test. This project applies all five deliberately.

| Letter | Principle | What It Means | Where You See It |
|--------|-----------|---------------|-----------------|
| **S** | Single Responsibility | One class = one job | `BookRepository` only does DB access. `LoggingBookRepository` only logs. `BookBuilder` only builds. |
| **O** | Open/Closed | Open for extension, closed for modification | Adding a new sort = new class, zero changes to `BookService`. Adding caching = new Decorator, `BookRepository` unchanged. |
| **L** | Liskov Substitution | Subtypes must be replaceable for their parent type | `LoggingBookRepository` can be used anywhere `IBookRepository` is expected. `AuthorSortStrategy` anywhere `ISortStrategy<Book>` is expected. |
| **I** | Interface Segregation | Interfaces should be small and focused | `IRepository<T>` has 5 methods. `ISpecification<T>` has 1 method. No interface forces implementors to have methods they don't need. |
| **D** | Dependency Inversion | High-level code depends on abstractions, not concretions | `BookService` depends on `IBookRepository`, never `BookRepository`. Pages depend on `IBookService`, never `BookService`. |

---

## Dependency Injection

**What is it?** Instead of a class creating its own dependencies with `new`,
it declares what it needs in its constructor and the framework provides it.

**Why does it matter?** It is the mechanism that makes every pattern in this
project practical. Without DI, you could not swap implementations, mock in tests,
or wire the decorator chain automatically.

```csharp
// Without DI — tightly coupled, untestable:
public class BookService
{
    private readonly BookRepository _repo = new BookRepository(new AppDbContext(...));
    // Can't replace BookRepository with a mock. Can't test without a database.
}

// With DI — loosely coupled, testable:
public class BookService
{
    private readonly IBookRepository _repo;
    public BookService(IBookRepository repo) { _repo = repo; }
    // DI provides whatever implements IBookRepository. Tests provide a mock.
}
```

**Program.cs is the Composition Root** — the one place where every
abstraction is wired to its concrete implementation.

```csharp
// The full wiring for the repository chain:
builder.Services.AddScoped<BookRepository>();                    // innermost concrete
builder.Services.AddScoped<LoggingBookRepository>(sp => ...);   // middle decorator
builder.Services.AddScoped<IBookRepository>(sp => ...);         // outermost — what gets injected

// Service wired to its interface:
builder.Services.AddScoped<IBookService, BookService>();

// Singleton — stateless, safe to share across all requests:
builder.Services.AddSingleton<IBookFactory, BookFactory>();
builder.Services.AddSingleton<ITokenService, TokenService>();
```

**Service lifetimes — this is important:**

| Lifetime | Created | Destroyed | Use when |
|----------|---------|-----------|----------|
| `Singleton` | Once at startup | App shuts down | Stateless services (factories, token generators) |
| `Scoped` | Once per HTTP request / Blazor circuit | Request ends | Services that share a `DbContext` |
| `Transient` | Every time it is requested | After use | Lightweight, stateless utilities |

**The most common mistake:** injecting a `Scoped` service into a `Singleton`.
The Scoped service gets "captured" and lives forever — `DbContext` leaks,
data gets stale. See [Background Service](#background-service) for the solution.

---

## REST API & JWT Authentication

The app exposes a full REST API alongside the Blazor UI.
Both the UI and the API share the same `IBookService` — zero duplication.

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | ❌ | Exchange credentials for a JWT token |
| GET | `/api/books` | ✅ | Get all books |
| GET | `/api/books/{id}` | ✅ | Get a single book |
| GET | `/api/books/search` | ✅ | Search with filters and sort |
| GET | `/api/books/paged` | ✅ | Paginated list |
| POST | `/api/books` | ✅ | Create a book |
| PUT | `/api/books/{id}` | ✅ | Update a book |
| DELETE | `/api/books/{id}` | ✅ | Delete a book |
| GET | `/api/recommendations` | ✅ | AI-powered recommendations |
| GET | `/health` | ❌ | Simple health check |
| GET | `/health/detail` | ❌ | Detailed health JSON |

### JWT Authentication Flow

JWT (JSON Web Token) is a stateless authentication mechanism. The server never
stores sessions — the token itself contains the user's identity, signed with a secret key.

```
Step 1: Client POSTs { username, password } to /api/auth/login
Step 2: Server validates credentials → generates signed JWT → returns token
Step 3: Client stores token, sends it on every future request:
        Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Step 4: JWT middleware validates the signature → populates HttpContext.User
Step 5: [Authorize] attribute allows or denies access based on that identity
```

**Why stateless?** The server does not need to store sessions in a database.
Any server instance can validate the token using the shared secret key.
This makes JWT ideal for distributed systems and microservices.

**Security note:** In this project, the JWT key is in `appsettings.json` for
development convenience. In production, it must come from environment variables
or a secrets manager — never committed to source control.

---

## FluentValidation

**Files:** `Validators/CreateBookValidator.cs`, `Validators/UpdateBookValidator.cs`

**Problem:** Validation logic ends up scattered — some in the controller, some in
the service, some nowhere. Error messages are inconsistent and hard to test.

**Solution:** FluentValidation gives each request its own dedicated validator class
with explicit, readable rules that produce consistent error messages.

```csharp
// CreateBookValidator.cs
public class CreateBookValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1000, DateTime.UtcNow.Year)
            .WithMessage($"Year must be between 1000 and {DateTime.UtcNow.Year}.")
            .When(x => x.Year.HasValue);  // ← Only validate if Year was provided
    }
}
```

**How it integrates:** `AddFluentValidationAutoValidation()` in Program.cs wires
FluentValidation into the ASP.NET Core model validation pipeline.
Invalid requests are automatically rejected with a `400 Bad Request` before
the controller action method ever runs.

**The response when validation fails:**
```json
{
  "errors": {
    "Title": ["Title is required."],
    "Year": ["Year must be between 1000 and 2026."]
  }
}
```

**Why not DataAnnotations?** DataAnnotations (`[Required]`, `[MaxLength]`) work,
but they put validation directly on the DTO class. For complex rules (conditional
validation, cross-field validation), they become impossible to read. FluentValidation
keeps DTOs clean and validation logic in one testable place.

---

## Pagination

**Files:** `DTOs/PaginatedResult.cs`, `DTOs/PagedBooksRequest.cs`
**Endpoint:** `GET /api/books/paged?pageNumber=1&pageSize=10&search=tolkien&status=Read&sortBy=Year`

**Problem:** `GET /api/books` returns every book in the database. With 10 books
this is fine. With 10,000 books you are sending megabytes of data the UI will
never display. It is slow, wasteful, and will crash mobile clients.

**Solution:** Return one page at a time, with metadata telling the client how to
navigate to other pages.

### The Request

```csharp
public class PagedBooksRequest
{
    public int    PageNumber  { get; init; } = 1;
    public int    PageSize    { get; init; } = 10;  // capped at 50
    public string? SearchTerm { get; init; }
    public string? Status     { get; init; }
    public string? SortBy     { get; init; }
}
```

### The Response

```json
{
  "items": [ /* the books for this page */ ],
  "totalCount": 247,
  "pageNumber": 3,
  "pageSize": 10,
  "totalPages": 25,
  "hasNextPage": true,
  "hasPreviousPage": true
}
```

### How It Works

```csharp
// BookService.GetPagedAsync():
// 1. Run the existing SearchAsync (filters + sort) — reuse, don't duplicate
var books = await SearchAsync(req.SearchTerm, parsedStatus, req.SortBy);

// 2. Paginate in memory
var items = books
    .Skip((req.PageNumber - 1) * req.PageSize)   // e.g., page 3, size 10 = skip 20
    .Take(req.PageSize)
    .ToList();

// 3. Return with metadata
return new PaginatedResult<Book>
{
    Items      = items,
    TotalCount = books.Count,
    PageNumber = req.PageNumber,
    PageSize   = req.PageSize
};
```

**TotalPages** is a computed property — never stored, always derived:
```csharp
public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
```

---

## Caching — The Decorator Chain

**Files:** `Repositories/CachingBookRepository.cs`, `Repositories/LoggingBookRepository.cs`

The full chain at runtime when `BookService` calls `GetAllAsync()`:

```
BookService.GetAllBooksAsync()
    ↓
CachingBookRepository.GetAllAsync()
    ├── Cache HIT  → return from IMemoryCache (no DB call)
    └── Cache MISS → call inner...
         ↓
         LoggingBookRepository.GetAllAsync()
             ├── Log "Getting all books..."
             ├── call inner...
             │    ↓
             │    BookRepository.GetAllAsync()
             │        ↓
             │        EF Core → SQLite → books.db
             │    ↑
             ├── Log "Got 42 books in 12ms"
             └── return books
         ↑
    ← store in cache for 60 seconds
    ↑
BookService gets the books
```

**Cache-aside strategy:**
- Read: check cache first. If present (HIT), return immediately. If not (MISS), fetch from DB, store in cache, return.
- Write: always go to DB first, then **invalidate** the cache so the next read gets fresh data.

**Why invalidate on write instead of update?**
Updating a cached collection correctly is complex. Invalidating and letting the
next read rebuild the cache is simpler and always correct.

---

## Health Checks

**Endpoints:** `GET /health` and `GET /health/detail`

**What is a health check?**
An endpoint that tells infrastructure (load balancers, container orchestrators,
monitoring dashboards) whether the application is alive and ready to serve traffic.

**Why does it matter?**
In production, Kubernetes and AWS ECS send HTTP requests to `/health` every few seconds.
If the response is not 200 OK, they route traffic away from that instance and restart it.
Without a health check, a broken app instance silently receives traffic and returns errors.

### Basic health check — `/health`

Returns `200 OK` with `Healthy` / `Degraded` / `Unhealthy` as plain text.

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");  // verifies DB is reachable

app.MapHealthChecks("/health");
```

### Detailed health check — `/health/detail`

Returns JSON with full status, per-check results, and timing:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": "4.2ms",
      "description": null
    }
  ],
  "totalDuration": "4.8ms"
}
```

This is what a monitoring dashboard (Grafana, Datadog, etc.) would consume.

---

## Global Exception Handling & ProblemDetails

**File:** `Middleware/GlobalExceptionMiddleware.cs`

**Problem:** Unhandled exceptions leak stack traces to clients in development
and return ugly, inconsistent error responses in production.

```json
// Without global exception handling — terrible:
System.NullReferenceException: Object reference not set to an instance of an object.
   at BookLibrary.Services.BookService.GetBookByIdAsync(Int32 id) in ...
```

**Solution:** One middleware wraps the entire pipeline. Any unhandled exception
is caught here and converted to a structured RFC 7807 ProblemDetails response.

### RFC 7807 — What Is It?

RFC 7807 is an internet standard for HTTP error responses. Instead of every
API inventing its own error format, everyone uses the same structure:

```json
{
  "status": 404,
  "title": "Resource Not Found",
  "detail": "No book with ID 99 was found.",
  "instance": "/api/books/99",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

**`traceId`** is critical for debugging in production — you give it to the developer
and they can find the exact log entry for that request.

### How the middleware works

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);  // run the rest of the pipeline
    }
    catch (Exception ex)
    {
        // Map exception types to HTTP status codes:
        // KeyNotFoundException   → 404 Not Found
        // ArgumentException      → 400 Bad Request
        // UnauthorizedAccess     → 401 Unauthorized
        // Everything else        → 500 Internal Server Error
        await WriteProblemDetailsAsync(context, ex);
    }
}
```

**Registered first in the pipeline** so it catches exceptions from every
other middleware and every controller action.

---

## Rate Limiting

**Built-in ASP.NET Core 7+ feature — no external library needed.**

**What is rate limiting?**
Restricting how many requests a client can make in a given time window.

**Why does it matter?**
- Prevents brute-force attacks on the login endpoint (try 10,000 passwords per second)
- Protects the API from accidental or malicious overload
- Ensures fair resource sharing between multiple clients

### Configuration

```csharp
builder.Services.AddRateLimiter(options =>
{
    // General API: 100 requests per minute per IP
    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 100;
        o.Window      = TimeSpan.FromMinutes(1);
    });

    // Auth endpoint: 10 requests per minute per IP (stricter — brute-force protection)
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window      = TimeSpan.FromMinutes(1);
    });
});
```

### Applied per-controller

```csharp
[EnableRateLimiting("api")]
public class BooksController : ControllerBase { ... }

[EnableRateLimiting("auth")]
public class AuthController : ControllerBase { ... }
```

### When the limit is exceeded

The client receives `429 Too Many Requests` with a `Retry-After: 60` header
and a structured JSON body explaining how long to wait.

### Fixed Window vs Sliding Window

**Fixed window:** The window resets at a fixed interval. Simple, predictable.
  Weakness: a client can burst at the boundary (100 at 00:59, 100 at 01:01).

**Sliding window:** The window slides with each request. Smoother, fairer.
  This project uses Fixed Window for simplicity; Sliding Window is available
  via `AddSlidingWindowLimiter()`.

---

## AI Book Recommendations

**Files:** `Services/BookRecommendationService.cs`, `Interfaces/IBookRecommendationService.cs`,
`Controllers/RecommendationsController.cs`, `Pages/Recommendations.razor`

**What does it do?**
Takes the user's read and currently-reading books, sends them to the Anthropic
Claude API, and gets back personalised book recommendations in JSON format.

### The Flow

```
User clicks "Get AI Recommendations"
    ↓
Recommendations.razor calls IBookRecommendationService.GetRecommendationsAsync()
    ↓
BookRecommendationService:
    1. Checks if API key is configured → if not, returns helpful message
    2. Gets the user's read/reading books
    3. Builds a prompt describing their reading history
    4. Sends prompt to Claude claude-3-5-haiku-20241022
    5. Parses the JSON response
    6. Returns BookRecommendationResult
    ↓
UI displays recommendation cards (title, author, genre, year, reason)
```

### The Prompt

The service sends Claude a structured prompt asking for JSON output:

```
You are a knowledgeable book recommendation engine.
Based on the user's reading history below, suggest 5 books they would enjoy.

USER'S READING HISTORY:
- "Clean Code" by Robert C. Martin (Programming) [2008] — Read
- "The Pragmatic Programmer" by David Thomas (Programming) [1999] — Read
...

Respond ONLY with valid JSON in this format:
{ "reasoning": "...", "recommendations": [{ "title": "...", ... }] }
```

### Graceful Degradation

The service handles every failure case without crashing:
- **No API key configured** → returns a message explaining how to add one
- **No books in reading history** → returns a message asking to add books first
- **API call fails** → logs the error, returns a friendly message
- **Response not valid JSON** → caught and logged

This is the correct pattern for any optional external dependency.

### Configuration

In `appsettings.json`:
```json
"Anthropic": {
  "ApiKey": "",
  "Model": "claude-3-5-haiku-20241022"
}
```

Set `ApiKey` to your Anthropic API key. Never commit real API keys to git.
Use environment variables in production: `Anthropic__ApiKey=sk-ant-...`

### Why an Interface?

`IBookRecommendationService` exists so:
- Unit tests can mock it — no real API calls in tests
- You can swap Claude for another AI provider by creating a different implementation
  and changing one line in Program.cs
- The Blazor page and the controller both depend on the abstraction, not the concrete class

---

## Options Pattern

**Problem:** Reading raw config strings like `_configuration["Jwt:Key"]` is fragile
(typo = silent null), not type-safe, not testable, and scattered everywhere.

**Solution:** Each feature has a dedicated strongly-typed class bound from `appsettings.json`.

```csharp
// Options class:
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key           { get; init; } = string.Empty;
    public string Issuer        { get; init; } = string.Empty;
    public string Audience      { get; init; } = string.Empty;
    public int    ExpiryMinutes { get; init; } = 60;
}

// Registration in Program.cs:
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// Consumption in any service:
public TokenService(IOptions<JwtOptions> options)
{
    _options = options.Value;  // fully typed, null-safe
}
```

| Class | Config Section | Used By |
|-------|---------------|---------|
| `JwtOptions` | `"Jwt"` | `TokenService`, JWT middleware |
| `CacheOptions` | `"Cache"` | `CachingBookRepository` |
| `LibraryStatsOptions` | `"LibraryStats"` | `LibraryStatsBackgroundService` |
| `AnthropicOptions` | `"Anthropic"` | `BookRecommendationService` |

---

## Middleware

Middleware is code that runs in the HTTP request pipeline — between the server
receiving a request and the controller handling it. Every request passes through
every registered middleware in order.

### RequestTimingMiddleware

**File:** `Middleware/RequestTimingMiddleware.cs`

Measures and logs elapsed milliseconds for every HTTP request.

```
Incoming Request
    ↓
[GlobalExceptionMiddleware]   ← outermost (catches everything)
    ↓
[RequestTimingMiddleware]     ← starts timer
    ↓
[Authentication]
    ↓
[Authorization]
    ↓
[Controller / Blazor]         ← actual work happens here
    ↑
[RequestTimingMiddleware]     ← stops timer, logs "GET /api/books → 200 in 14ms"
    ↑
[GlobalExceptionMiddleware]   ← no exception → passes response through
    ↑
Response sent to client
```

Blazor SignalR connections (`/_blazor`) are skipped — they are long-lived WebSocket
connections and logging timing on them would generate thousands of meaningless log entries.

### GlobalExceptionMiddleware

Wraps the entire pipeline. Catches any unhandled exception and converts it to
a ProblemDetails response. See [Global Exception Handling](#global-exception-handling--problemdetails).

### Extension Method Pattern

Both middleware classes are registered via extension methods:

```csharp
// Instead of:
app.UseMiddleware<RequestTimingMiddleware>();

// You write:
app.UseRequestTiming();
```

This is the conventional ASP.NET Core pattern. It is cleaner to read and
hides the implementation detail of which class backs the extension.

---

## Background Service

**File:** `BackgroundServices/LibraryStatsBackgroundService.cs`

A background task that runs for the lifetime of the application, logging
library statistics (total books, by status) on a configurable interval.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        await LogStatsAsync(stoppingToken);
    }
}
```

### The Scoped-in-Singleton Problem

This is one of the most important concepts in this codebase.

`AddHostedService<T>` registers the service as a **Singleton** — it lives for
the entire application lifetime. `IBookService` is **Scoped** — it should live
only for the duration of one request.

If you inject `IBookService` directly into the background service constructor,
the Scoped service gets captured in the Singleton. The same `DbContext` is
shared across all future work — leading to data staleness and memory leaks.

**The fix: `IServiceScopeFactory`**

```csharp
// Constructor — inject the factory (Singleton-safe), not the service (Scoped):
public LibraryStatsBackgroundService(IServiceScopeFactory scopeFactory, ...)

// Per tick — create a fresh scope, resolve the service, let it dispose:
await using var scope = _scopeFactory.CreateAsyncScope();
var bookService = scope.ServiceProvider.GetRequiredService<IBookService>();
await bookService.GetAllBooksAsync();
// scope is disposed here — DbContext is cleaned up correctly
```

This pattern applies to any background work that needs database access.

---

## UI Layer — Blazor

Blazor Server is a framework where UI components run on the server and
communicate with the browser over a WebSocket connection (SignalR).
The browser renders HTML; C# handles the logic.

### Component Architecture

Each component has a single responsibility — the same SOLID principles applied to the UI.

| Component | Responsibility |
|-----------|---------------|
| `MainLayout.razor` | App shell — sidebar + main content slot. No business logic. |
| `NavMenu.razor` | Navigation links + theme toggle. Handles JS interop for theme. |
| `Index.razor` | Dashboard — stats, currently reading, recently added. |
| `Books.razor` | Book list with search, filter, sort, delete with confirm dialog. |
| `BookForm.razor` | Add/edit form. Uses `IBookFactory` + `IBookService`. |
| `Recommendations.razor` | AI recommendations page. Calls `IBookRecommendationService`. |
| `Toast.razor` | Dumb (presentational) component — receives props, renders, nothing else. |
| `ConfirmDialog.razor` | Modal dialog — raises `OnConfirm` / `OnCancel` EventCallbacks. |

### Component Communication

**Parent → Child (Parameters):**
```razor
<Toast IsVisible="@showToast" Message="@toastMessage" Type="success" />
```

**Child → Parent (EventCallback):**
```csharp
// ConfirmDialog declares what events it raises:
[Parameter] public EventCallback OnConfirm { get; set; }
[Parameter] public EventCallback OnCancel  { get; set; }

// The parent decides what happens:
<ConfirmDialog OnConfirm="DeleteConfirmed" OnCancel="CancelDelete" />
```

The dialog does not know what "confirm" does — the parent decides. This is
Dependency Inversion applied to the UI layer.

### Dark / Light Theme

1. `_Host.cshtml` runs an inline script before the page renders, reading
   `localStorage` and setting `data-theme` on `<html>`. This prevents a flash.
2. `site.css` defines a `[data-theme="light"]` block overriding every color variable.
3. `NavMenu.razor` toggles the attribute via `IJSRuntime` and persists the preference.

**JS Interop rule:** Browser APIs are only available after Blazor hydrates.
Always use `OnAfterRenderAsync` with a `firstRender` guard — never `OnInitializedAsync`.

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var saved = await JS.InvokeAsync<string>("themeInterop.getTheme");
        isDarkTheme = saved != "light";
        StateHasChanged();
    }
}
```

### CSS Design System

All visual tokens are CSS custom properties in `site.css`:

```css
:root {
    --bg-base:    #0D0D14;    /* page background */
    --accent:     #7C3AED;    /* primary purple  */
    --text:       #F1F0FF;    /* primary text    */
}
[data-theme="light"] {
    --bg-base:    #F4F4F8;    /* just override the tokens */
    --text:       #1A1A2E;
}
```

No component hard-codes a color. Every component uses variables. Changing the
entire visual theme means changing one CSS block, not touching any component.

---

## Unit Testing

**42 tests — xUnit + Moq.**

Tests verify behaviour, not implementation. A test should not care which
concrete class runs — it cares that the result is correct.

```csharp
// Arrange: replace IBookRepository with a Moq fake
var repoMock = new Mock<IBookRepository>();
repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Book> { book1, book2 });

// Act: call the real BookService
var service = new BookService(repoMock.Object, strategies);
var result  = await service.GetAllBooksAsync();

// Assert: check the result
Assert.Equal(2, result.Count);

// Verify: confirm the mock was called correctly
repoMock.Verify(r => r.GetAllAsync(), Times.Once);
```

**Why mock `IBookRepository` and not `BookRepository`?**
`BookService` depends on `IBookRepository` (the interface). Mocking the interface
means the test never touches EF Core, SQLite, or any infrastructure. Tests are fast,
isolated, and reliable. This only works because of Dependency Inversion.

**Testing the Caching Decorator:**
`IMemoryCache` is complex to mock. A real `MemoryCache` is used instead —
this is an example of preferring real collaborators when mocking is harder than
just using the real thing.

**Run tests:**
```bash
dotnet test
```

---

## Middleware Pipeline Order — Why It Matters

This is where many developers make mistakes. Order is everything.

```csharp
// Program.cs — the order these lines appear = the order they run:

app.UseGlobalExceptionHandler();  // 1st — catches exceptions from everything below

app.UseSwagger();                 // Dev only
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();                 // Must come before rate limiter and auth

app.UseRateLimiter();             // After routing (needs route info), before auth

app.UseAuthentication();          // Establishes WHO the user is (reads JWT)
app.UseAuthorization();           // Decides WHAT they can do ([Authorize] checks)

app.UseRequestTiming();           // Custom timing middleware

app.MapHealthChecks("/health");
app.MapControllers();             // Must come BEFORE MapFallbackToPage
app.MapBlazorHub();
app.MapFallbackToPage("/_Host"); // Catches everything not matched above
```

**Why `UseAuthentication` before `UseAuthorization`?**
Authentication establishes identity. Authorization evaluates permissions.
You cannot check permissions before you know who the user is.

**Why `MapControllers` before `MapFallbackToPage`?**
The fallback route (`/_Host`) matches *everything*. If controllers are registered
after, their routes will never be reached — the fallback wins every time.

**Why `UseGlobalExceptionHandler` first?**
If it is not first, exceptions thrown by other middleware (like `UseRouting`)
will not be caught and will bubble up as unhandled crashes.

---

## How to Add a New Feature

### Add a new entity (e.g., Author)

1. Create `Models/Author.cs` extending `BaseEntity`
2. Create `Interfaces/IAuthorRepository.cs` extending `IRepository<Author>`
3. Create `Repositories/AuthorRepository.cs` extending `Repository<Author>`
4. Create `Interfaces/IAuthorService.cs` with business operations
5. Create `Services/AuthorService.cs` implementing `IAuthorService`
6. Create `DTOs/AuthorDto.cs`, `CreateAuthorRequest.cs`
7. Create `Validators/CreateAuthorValidator.cs`
8. Create `Controllers/AuthorsController.cs`
9. Add `DbSet<Author>` to `AppDbContext`
10. Wire everything in `Program.cs`

Steps 1–8 touch zero existing files. Step 9 adds one property. Step 10 adds registrations.
No existing code is modified. This is the Open/Closed Principle in action.

### Add a new sort strategy

1. Create `Strategies/GenreSortStrategy.cs` implementing `ISortStrategy<Book>`
2. Register it in `Program.cs`: `builder.Services.AddScoped<ISortStrategy<Book>, GenreSortStrategy>()`
3. Done — `BookService` and the UI automatically discover it

Zero changes to existing files.

### Add a new filter specification

1. Create `Specifications/GenreSpecification.cs` implementing `ISpecification<Book>`
2. Use it in `BookService.SearchAsync()` or compose with `AndSpecification`

---

## Key Takeaways

If you read nothing else, read this section.

**1. Program.cs is the map of the entire application.**
Every dependency, every wiring decision, every lifetime choice lives there.
When you are confused about where something comes from, read Program.cs.

**2. Interfaces are the connective tissue.**
The UI talks to `IBookService`. The service talks to `IBookRepository`.
The controller talks to `IBookService`. Nobody talks to concrete classes.
This is what makes everything testable, swappable, and maintainable.

**3. Patterns solve specific problems.**
Strategy = eliminate if/else chains. Specification = name and reuse filter rules.
Decorator = add behaviour without modifying existing code.
Do not use a pattern because it sounds impressive — use it because it solves a real problem.

**4. DI is the glue, not the goal.**
Dependency Injection makes patterns practical. Without it, wiring the decorator chain
manually in every class would be unmanageable. DI is infrastructure, not architecture.

**5. Middleware pipeline order is critical and silent.**
Getting the order wrong does not always produce an error. Sometimes it just
silently breaks authentication, rate limiting, or exception handling in ways
that only appear in production. Know the order, know why.

**6. Scoped services cannot live in Singletons.**
This is the most common runtime bug in ASP.NET Core. Always use `IServiceScopeFactory`
when a background service or singleton needs database access.

**7. Never commit secrets to git.**
API keys, JWT signing keys, connection strings with passwords — use environment
variables or a secrets manager. `appsettings.json` is for structure, not secrets.

**8. Graceful degradation is a feature.**
The AI recommendation service returns a helpful message when unconfigured, not a crash.
Every optional external dependency should behave this way.

**9. Health checks are not optional in production.**
Without them, broken instances silently receive traffic. One endpoint, five lines of code,
massive operational value.

**10. Test behaviour, not implementation.**
Your tests should not break when you refactor internals. Test through interfaces.
Mock dependencies, not the class under test.
