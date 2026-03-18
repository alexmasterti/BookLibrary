# BookLibrary Architecture Guide

> **Who this is for:** Junior to mid-level developers who want to understand modern .NET patterns, why they exist, and how they connect. Every section answers: *What is it? Why does it exist? How does it work here? What to learn next?*

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Solution Structure](#2-solution-structure)
3. [Design Patterns](#3-design-patterns)
   - Repository Pattern
   - Decorator Pattern
   - Strategy Pattern
   - Specification Pattern
   - Factory + Builder Pattern
   - Options Pattern
4. [SOLID Principles](#4-solid-principles)
5. [API Design](#5-api-design)
   - REST Controllers
   - JWT Authentication
   - FluentValidation
   - Rate Limiting
   - ProblemDetails (RFC 7807)
   - API Versioning
6. [Data Layer](#6-data-layer)
   - Entity Framework Core + SQLite
   - Repository Abstraction
   - In-Memory Caching
7. [Observability](#7-observability)
   - Serilog Structured Logging
   - Health Checks
   - OpenTelemetry (Traces + Metrics)
8. [Resilience](#8-resilience)
   - Polly Resilience Pipeline
   - Response Compression
9. [CQRS + MediatR](#9-cqrs--mediatr)
10. [AI Integration](#10-ai-integration)
11. [Background Services](#11-background-services)
12. [Testing](#12-testing)
    - Unit Tests
    - Integration Tests
13. [Infrastructure](#13-infrastructure)
    - Docker + docker-compose
    - GitHub Actions CI/CD
    - Railway Deployment
14. [.NET Aspire (Coming Soon)](#14-net-aspire-coming-soon)
15. [Learning Path](#15-learning-path)

---

## 1. Project Overview

BookLibrary is a **full-stack .NET 8 application** that manages a personal book collection. It has two interfaces:

- **Blazor Server UI** — a real-time, component-based web UI running on the server
- **REST API** — a JSON API secured with JWT tokens, consumed by external clients

**Technology stack:**
| Layer | Technology |
|-------|------------|
| UI | Blazor Server (.NET 8) |
| API | ASP.NET Core Web API |
| Database | SQLite via Entity Framework Core 8 |
| Auth | JWT Bearer Tokens |
| AI | Anthropic Claude API |
| Logging | Serilog |
| Observability | OpenTelemetry |
| Resilience | Polly |
| Testing | xUnit + Moq + WebApplicationFactory |
| CI/CD | GitHub Actions + Railway |

**Why Blazor Server instead of Blazor WebAssembly?**
Blazor Server runs on the server and pushes UI updates to the browser via SignalR. This is faster to start (no large WASM download), works on older browsers, and keeps business logic server-side. The tradeoff: requires constant network connection and scales less easily under heavy load. For a personal library app, this tradeoff is fine.

---

## 2. Solution Structure

```
BookLibrary/
├── BackgroundServices/       # Long-running tasks (stats logger)
├── Builders/                 # BookBuilder — fluent, validated construction
├── Controllers/              # REST API endpoints
│   ├── V2/                   # API Version 2 controllers
│   ├── AuthController        # POST /api/auth/login
│   ├── BooksController       # CRUD for books (V1)
│   ├── BooksCqrsController   # Same CRUD but via MediatR/CQRS
│   └── RecommendationsController
├── CQRS/                     # Commands, Queries, Handlers (MediatR)
│   ├── Commands/             # CreateBookCommand, DeleteBookCommand
│   ├── Handlers/             # One handler per command/query
│   └── Queries/              # GetAllBooksQuery, SearchBooksQuery, etc.
├── Data/                     # EF Core DbContext + migrations
├── DTOs/                     # Data Transfer Objects (API shapes)
│   └── V2/                   # V2-only DTOs with computed fields
├── Factories/                # BookFactory — delegates to BookBuilder
├── Interfaces/               # Abstractions (the 'I' prefix interfaces)
├── Middleware/                # Request pipeline extensions
│   ├── GlobalExceptionMiddleware
│   └── RequestTimingMiddleware
├── Models/                   # Domain models (Book, BaseEntity, enums)
├── Options/                  # Typed configuration classes
├── Pages/                    # Blazor pages (.razor files)
├── Repositories/             # Data access implementations + Decorators
├── Services/                 # Business logic services
│   ├── BookLibraryTelemetry  # Custom tracing spans
│   ├── BookRecommendationService
│   ├── BookService
│   └── TokenService
├── Shared/                   # Blazor shared components (Layout, Toast, etc.)
├── Specifications/           # Composable filter rules
├── Strategies/               # Sort algorithms (pluggable)
├── Validators/               # FluentValidation rules
├── Program.cs                # Composition root — ALL wiring happens here
├── BookLibrary.Tests/        # Unit + Integration tests
└── ARCHITECTURE.md           # You are here
```

---

## 3. Design Patterns

### Repository Pattern

**What it is:** A class that hides the database from the rest of the app. Instead of writing `dbContext.Books.Where(...).ToList()` everywhere, you call `bookRepository.GetAllAsync()`.

**Why it exists:**
- Centralizes all data access in one place
- Makes services testable (you can mock a repository without needing a real database)
- Lets you swap databases later without changing business logic

**How it works here:**

```
IBookRepository (interface)
   ↑ implemented by
BookRepository (concrete — uses EF Core + SQLite)
```

The interface lives in `Interfaces/` and defines the contract. Business logic (`BookService`) only sees the interface — never the concrete class.

**Code example:**
```csharp
// Interface (what callers see)
public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(int id);
    Task AddAsync(Book book);
    Task UpdateAsync(Book book);
    Task DeleteAsync(int id);
}

// Concrete (hidden behind the interface)
public class BookRepository : IBookRepository
{
    private readonly AppDbContext _db;
    // ... uses _db.Books to actually query SQLite
}
```

**What to learn next:** Generic Repository pattern, Unit of Work pattern, Domain-Driven Design (DDD) Repositories.

---

### Decorator Pattern

**What it is:** Wrapping an object with another object that adds behavior, without changing the original object or its callers.

**Why it exists:**
- Adds cross-cutting concerns (caching, logging) without polluting core logic
- Each decorator does ONE thing (Single Responsibility)
- Decorators compose — you can stack them in any order

**How it works here:**

```
CachingBookRepository (outer — checks cache first, wraps...)
    → LoggingBookRepository (middle — logs before/after, wraps...)
        → BookRepository (inner — actual database calls)
```

When `BookService` calls `_repository.GetAllAsync()`, the call goes through all three layers:

1. **CachingBookRepository** — checks MemoryCache. If hit, return cached data immediately (no DB call). If miss, call the next layer.
2. **LoggingBookRepository** — logs "Getting books from DB..." then calls BookRepository, then logs the result.
3. **BookRepository** — actually queries SQLite.

**The wiring in Program.cs:**
```csharp
// Register concrete BookRepository (innermost)
builder.Services.AddScoped<BookRepository>();

// Register LoggingBookRepository wrapping BookRepository
builder.Services.AddScoped<LoggingBookRepository>(sp =>
    new LoggingBookRepository(
        sp.GetRequiredService<BookRepository>(),
        sp.GetRequiredService<ILogger<LoggingBookRepository>>()));

// Register CachingBookRepository (outermost) as IBookRepository
// This is what all callers receive when they ask for IBookRepository
builder.Services.AddScoped<IBookRepository>(sp =>
    new CachingBookRepository(
        sp.GetRequiredService<LoggingBookRepository>(), ...));
```

**What to learn next:** Chain of Responsibility pattern, AOP (Aspect-Oriented Programming), Middleware pattern (which is a form of Decorator).

---

### Strategy Pattern

**What it is:** Define a family of algorithms, put each in its own class, and make them interchangeable at runtime.

**Why it exists:**
- Avoids giant if/switch chains for different behaviors
- Each strategy is independently testable
- Adding a new algorithm doesn't change existing code (Open/Closed Principle)

**How it works here:**

```csharp
// The interface (what BookService uses)
public interface ISortStrategy<T>
{
    string Name { get; }
    IQueryable<T> Sort(IQueryable<T> query);
}

// Three concrete strategies
public class TitleSortStrategy  : ISortStrategy<Book> { /* OrderBy Title */ }
public class AuthorSortStrategy : ISortStrategy<Book> { /* OrderBy Author */ }
public class YearSortStrategy   : ISortStrategy<Book> { /* OrderBy Year */ }
```

When a user requests `GET /api/books/search?sort=author`, `BookService` looks up the matching strategy by name and calls `.Sort(query)` on it. The service doesn't know or care which algorithm runs.

**What to learn next:** Command pattern, State pattern, Template Method pattern.

---

### Specification Pattern

**What it is:** Encapsulate a filter rule in its own class, and combine rules with AND/OR/NOT operators.

**Why it exists:**
- Composable — `new StatusSpec(Reading).And(new TitleContains("dragon"))`
- Testable — you can test each filter rule in isolation
- Reusable — the same spec can be used in the API and in background jobs

**How it works here:**

```csharp
// Base abstraction
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);
    public Specification<T> And(Specification<T> other)
        => new AndSpecification<T>(this, other);
}

// Example concrete spec
public class StatusSpecification : Specification<Book>
{
    private readonly ReadingStatus _status;
    public bool IsSatisfiedBy(Book book) => book.Status == _status;
}
```

Usage in `BookService.SearchAsync`:
```csharp
Specification<Book> spec = new TitleOrAuthorContainsSpecification(query);
if (status.HasValue)
    spec = spec.And(new StatusSpecification(status.Value));

return await _repository.SearchAsync(spec);
```

**What to learn next:** Query Specification with EF Core `IQueryable`, Domain events, Aggregate roots.

---

### Factory + Builder Pattern

**What it is:**
- **Builder** constructs complex objects step-by-step with validation
- **Factory** provides a simple creation API that delegates to the Builder

**Why it exists:**
- Centralizes creation logic — if you need to change how a Book is created, you change ONE place
- Builder validates inputs before creating the object (no invalid `Book` can exist)
- Factory provides a clean interface (callers don't need to know about the Builder)

**How it works here:**

```csharp
// Builder — fluent API with validation
public class BookBuilder
{
    private string _title = "";
    private string _author = "";

    public BookBuilder WithTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");
        _title = title;
        return this;
    }

    public Book Build() => new Book { Title = _title, Author = _author, ... };
}

// Factory — simple API for controllers/services to use
public class BookFactory : IBookFactory
{
    public Book Create(string title, string author, ...) =>
        new BookBuilder()
            .WithTitle(title)
            .WithAuthor(author)
            .Build();
}
```

**What to learn next:** Abstract Factory, Prototype pattern, Object Mother pattern for tests.

---

### Options Pattern

**What it is:** Bind JSON configuration to a strongly-typed C# class.

**Why it exists:**
- No magic strings — `"Jwt:Key"` becomes `jwtOptions.Key`
- IntelliSense and compile-time safety
- Validated at startup rather than at runtime
- Easy to test (just create an instance with test values)

**How it works here:**

```json
// appsettings.json
{
  "Jwt": {
    "Key": "your-secret-key",
    "ExpiryMinutes": 60
  }
}
```

```csharp
// Options class
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

// Registration in Program.cs
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// Injection in TokenService
public class TokenService
{
    private readonly JwtOptions _options;
    public TokenService(IOptions<JwtOptions> options)
        => _options = options.Value;
}
```

**What to learn next:** `IOptionsSnapshot` (per-request reload), `IOptionsMonitor` (live reload), data annotations validation on options, `ValidateOnStart()`.

---

## 4. SOLID Principles

**S — Single Responsibility:** Each class has ONE reason to change.
- `BookService` handles business logic, not data access or HTTP concerns
- `TokenService` only generates JWT tokens
- `BookBuilder` only constructs and validates Books

**O — Open/Closed:** Open for extension, closed for modification.
- Adding a new sort strategy? Create a new class. Don't touch `BookService`.
- Adding new filter rules? Create a new `Specification`. Don't touch existing ones.

**L — Liskov Substitution:** Subtypes must be substitutable for base types.
- `CachingBookRepository` can replace `BookRepository` anywhere `IBookRepository` is expected

**I — Interface Segregation:** Don't force classes to depend on methods they don't use.
- `IBookRepository` is separate from `IBookService`
- Controllers depend on `IBookService`, not directly on `IBookRepository`

**D — Dependency Inversion:** High-level modules depend on abstractions, not concretions.
- `BookService` depends on `IBookRepository` (interface), not `BookRepository` (class)
- Controllers depend on `IBookService`, not `BookService`
- All dependencies injected via constructor (Dependency Injection)

---

## 5. API Design

### REST Controllers

**What REST means in practice:**
- `GET /api/books` — retrieve list (safe, idempotent, no side effects)
- `POST /api/books` — create a book (returns 201 Created + Location header)
- `PUT /api/books/{id}` — replace a book fully (idempotent)
- `DELETE /api/books/{id}` — remove a book (idempotent)

**Controllers in this project:**
- `AuthController` — `POST /api/auth/login` → returns JWT
- `BooksController` — Full CRUD for books (V1)
- `BooksV2Controller` — Same operations, enriched responses (V2)
- `BooksCqrsController` — Same operations via MediatR
- `RecommendationsController` — AI book suggestions

**What to learn next:** HATEOAS (hypermedia links in responses), GraphQL as an alternative to REST, gRPC for service-to-service communication.

---

### JWT Authentication

**What it is:** JSON Web Tokens — a compact, signed token that proves identity without server-side session state.

**Why it exists:**
- Stateless — the server doesn't store sessions, so it scales horizontally
- Self-contained — the token carries claims (user ID, roles, expiry)
- Signed — tamper-proof (HMAC-SHA256 signature)

**How it works:**

```
Client: POST /api/auth/login { username, password }
Server: validates credentials → generates JWT → returns token

Client: GET /api/books  (Authorization: Bearer eyJ...)
Server: validates token signature → extracts claims → allows/denies
```

**The token is three Base64-encoded parts separated by dots:**
```
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.abc123
[   Header          ] [    Payload         ] [Sig]
```

**In production, also use:**
- HTTPS everywhere (tokens in transit must be encrypted)
- Short expiry (15–60 minutes) + refresh tokens
- ASP.NET Identity for user management
- Token revocation list for logout

**What to learn next:** OAuth 2.0, OpenID Connect, ASP.NET Identity, Refresh Tokens, PKCE flow.

---

### FluentValidation

**What it is:** A validation library that lets you express validation rules as fluent code rather than data annotations.

**Why it exists:**
- Separation of concerns — validation rules live outside the model
- Rich rule composition — `NotEmpty().MaximumLength(200).WithMessage("...")`
- Automatic integration — invalid requests return 400 before hitting your controller

**How it works here:**

```csharp
public class CreateBookValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1, DateTime.Now.Year)
            .When(x => x.Year.HasValue);
    }
}
```

When `POST /api/books` arrives with `{ "title": "", "author": "..." }`, FluentValidation fires, validation fails, and the response is:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required."]
  }
}
```

**What to learn next:** Custom validators, cross-property validation, async validators, using FluentValidation with Blazor.

---

### Rate Limiting

**What it is:** Throttling — limiting how many requests a client can make in a time window.

**Why it exists:**
- Prevents abuse and DDoS attacks
- Protects the auth endpoint from brute-force password guessing
- Ensures fair resource sharing between clients

**Two policies in this app:**
- `"api"` — 100 requests per minute per IP (generous for normal use)
- `"auth"` — 10 requests per minute per IP (strict for login)

**How it works:**
```csharp
// Registration
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromMinutes(1);
    });
});

// Per-endpoint attribute
[EnableRateLimiting("api")]
public class BooksController { ... }
```

When rate limit is exceeded: `429 Too Many Requests` with `Retry-After: 60` header.

**What to learn next:** Sliding window limiter, token bucket limiter, distributed rate limiting with Redis, `IPartitionedRateLimiter` for per-user limits.

---

### ProblemDetails (RFC 7807)

**What it is:** A standard JSON error format for HTTP APIs. Instead of custom error shapes, all errors follow the same structure.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Book with ID 42 was not found.",
  "instance": "/api/books/42",
  "traceId": "00-abc123..."
}
```

**Why it exists:**
- Clients have ONE error-handling format to implement
- Tooling (Swagger, API clients) understands standard fields
- `traceId` links the error to server-side logs

**What to learn next:** Custom ProblemDetails types (e.g., `ValidationProblemDetails`), middleware-based ProblemDetails in ASP.NET Core 7+.

---

### API Versioning

**What it is:** Maintaining multiple versions of an API simultaneously so old clients don't break when you add new features.

**Why it exists:**
- Real-world APIs have clients you don't control (mobile apps, third-party integrations)
- You can't force all clients to update simultaneously
- Versioning lets you add new fields, change behavior, or deprecate endpoints gradually

**How it works here:**

Three ways to specify the version:
```
URL segment:   GET /api/v2/books
Header:        GET /api/books  +  X-API-Version: 2.0
Query string:  GET /api/books?api-version=2.0
```

**V1 vs V2 response comparison:**
```json
// V1: /api/v1/books/1
{ "id": 1, "title": "Dune", "author": "Herbert", "createdAt": "2024-01-15" }

// V2: /api/v2/books/1 — same data + computed fields
{
  "id": 1, "title": "Dune", "author": "Herbert", "createdAt": "2024-01-15",
  "daysInLibrary": 428,
  "isRecentlyAdded": false,
  "era": "Modern"
}
```

V2's computed fields (`DaysInLibrary`, `IsRecentlyAdded`, `Era`) are calculated on the fly from `CreatedAt` and `Year`. They're NOT stored in the database — they're added to `BookDtoV2` as computed properties.

**What to learn next:** API deprecation strategies, sunset headers, semantic versioning, header-based content negotiation.

---

## 6. Data Layer

### Entity Framework Core + SQLite

**What EF Core is:** An Object-Relational Mapper (ORM) that lets you work with databases using C# objects instead of raw SQL.

**Why SQLite for development:**
- Zero setup — no separate database server to install
- File-based — the whole database is `books.db` in the project root
- Good enough for apps with light concurrency
- Easy to swap for PostgreSQL or SQL Server in production (just change the connection string and NuGet package)

**The DbContext:**
```csharp
public class AppDbContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Book>().HasKey(b => b.Id);
        builder.Entity<Book>().Property(b => b.Title).IsRequired().HasMaxLength(200);
        // etc.
    }
}
```

**Migrations vs EnsureCreated:**
This project uses `db.Database.EnsureCreated()` at startup — it creates the schema if it doesn't exist. This is fine for development. In production, you'd use migrations (`dotnet ef migrations add`, `dotnet ef database update`) for controlled schema evolution.

**What to learn next:** EF Core migrations, query optimization, compiled queries, `AsNoTracking()` for read-only queries, database sharding.

---

### In-Memory Caching

**What it is:** Storing frequently-read data in RAM to avoid database round-trips.

**The pattern used:** Cache-Aside (also called Lazy Loading)

```
1. Check cache for "books"
2. If FOUND (cache hit): return cached data immediately
3. If NOT FOUND (cache miss): 
   a. Query database
   b. Store result in cache
   c. Return data
```

**Cache invalidation on writes:**
When a book is added, updated, or deleted, the `CachingBookRepository` clears the relevant cache entries. This ensures clients never see stale data for more than the cache duration.

**Configured TTL (Time To Live):**
```json
"Cache": {
  "BooksCacheDurationSeconds": 60
}
```

**What to learn next:** Distributed caching with Redis (`IDistributedCache`), cache eviction policies, sliding expiration, cache stampede prevention.

---

## 7. Observability

### Serilog Structured Logging

**What it is:** Structured logging means log events are structured data (key-value pairs), not just text strings.

**Why structured logging matters:**
```
// Traditional string log (hard to query):
"INFO: User admin fetched 42 books in 123ms"

// Structured log (machine-readable, filterable):
{ "Level": "Info", "Action": "GetBooks", "User": "admin", "Count": 42, "ElapsedMs": 123 }
```

With structured logs, you can query: "Show me all requests that fetched more than 100 books" or "Show me all requests slower than 500ms" — impossible with plain text logs.

**How Serilog is configured here:**

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)  // Quiet Microsoft internals
    .Enrich.FromLogContext()       // Add request context
    .Enrich.WithMachineName()      // Which server logged this
    .Enrich.WithEnvironmentName()  // dev/staging/prod
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}")
    .WriteTo.File("logs/booklibrary-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Log sinks:** Where logs are sent
- **Console** — visible during development, ingested by Docker/Kubernetes logging drivers
- **File** — `logs/booklibrary-2024-01-15.log` with 7-day retention

**In production, you'd add:**
```csharp
.WriteTo.Seq("http://localhost:5341")     // Seq - local dev search
.WriteTo.Elasticsearch(...)               // Elasticsearch + Kibana
.WriteTo.ApplicationInsights(...)         // Azure Monitor
.WriteTo.DatadogLogs(...)                 // Datadog
```

**Request logging middleware:**
```
POST /api/auth/login responded 200 in 45.2ms
GET /api/books responded 200 in 8.7ms
```

**What to learn next:** Seq (local structured log explorer), ELK Stack (Elasticsearch + Logstash + Kibana), log correlation IDs, structured logging best practices.

---

### Health Checks

**What they are:** Endpoints that report whether your app and its dependencies are healthy.

**Why they exist:**
- Load balancers use them to route traffic away from unhealthy instances
- Kubernetes uses them for readiness and liveness probes
- Monitoring dashboards use them for alerts
- Docker healthcheck commands use them

**Two endpoints:**
```
GET /health          → Simple: "Healthy" or "Unhealthy" (used by load balancers)
GET /health/detail   → JSON: which checks passed/failed and why
```

**Current checks:**
- `database` — `EF Core` pings SQLite to confirm it's reachable

**Example /health/detail response:**
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy", "duration": "2.3ms" }
  ],
  "totalDuration": "3.1ms"
}
```

**What to learn next:** Custom health checks, adding checks for Redis/message queues/external APIs, health check UI (AspNetCore.HealthChecks.UI), Kubernetes probes.

---

### OpenTelemetry (Traces + Metrics)

**What it is:** An open-source observability framework that captures distributed traces and metrics. It's the industry standard for instrumenting cloud-native applications.

**The 3 pillars of observability:**
```
Logs    = WHAT happened ("Error connecting to database at 14:32")
Metrics = HOW MUCH/OFTEN (200 requests/min, 99th percentile latency 450ms)
Traces  = THE JOURNEY of one request (which functions were called, in what order, how long each took)
```

**Serilog handles Logs. OpenTelemetry handles Metrics and Traces.**

**Traces explained:**
A trace is like a call stack across time. Each step is a "span":
```
Trace: GET /api/books (450ms total)
  ├── Middleware pipeline (5ms)
  ├── BooksController.GetAll (440ms)
  │     ├── CachingBookRepository.GetAllAsync (3ms) — cache miss
  │     │     ├── LoggingBookRepository.GetAllAsync (430ms)
  │     │     │     └── BookRepository.GetAllAsync — SQL query (425ms)
  └── Serialize response (5ms)
```

**Metrics explained:**
Counters, gauges, and histograms:
- Requests per second
- Response time percentiles (P50, P95, P99)
- Active connections
- Cache hit rate

**In this project:**
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()  // Auto-traces every HTTP request
        .AddHttpClientInstrumentation()  // Traces outbound HTTP calls
        .AddConsoleExporter())           // Dev: print to console
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter());
```

**Custom spans with `BookLibraryTelemetry`:**
```csharp
using var activity = BookLibraryTelemetry.StartBookOperation("GetBookById", bookId: id);
// Your code runs here
// The activity is automatically closed when disposed
```

**In production, swap ConsoleExporter for:**
- **OTLP → Jaeger** (open-source trace visualization)
- **OTLP → Zipkin** (another open-source option)
- **OTLP → Datadog / New Relic / Honeycomb** (commercial APM)
- **Azure Application Insights** (Azure-native)

**What to learn next:** Jaeger, Prometheus + Grafana, OpenTelemetry Collector, distributed tracing in microservices.

---

## 8. Resilience

### Polly Resilience Pipeline

**What it is:** A library for handling transient failures in distributed systems.

**Why it exists:**
Network calls fail. External APIs have downtime, rate limits, and flaky behavior. Without resilience patterns, one bad network call crashes your entire request. Polly adds automatic recovery strategies.

**Three strategies used here (in `BookRecommendationService`):**

#### Retry with Exponential Backoff
```
Attempt 1 → fails → wait 2s
Attempt 2 → fails → wait 4s
Attempt 3 → fails → wait 8s
Attempt 4 → fail → give up, throw exception
```

Why exponential backoff? If the API is overloaded, hammering it every 100ms makes it worse. Waiting longer gives the service time to recover.

#### Circuit Breaker
```
CLOSED state: requests flow normally
  → If 50%+ fail in 30s window (min 3 requests):
OPEN state: all requests fail immediately (no network call) for 30s
  → After 30s break:
HALF-OPEN state: try one request
  → If succeeds → back to CLOSED
  → If fails → back to OPEN
```

Why circuit breaker? Without it, every request to a failing service waits for a timeout (15s here), consuming threads and degrading performance. With circuit breaker, requests fail fast when the service is known to be down.

#### Timeout
Each individual attempt has a 15-second budget. Without this, a hung HTTP call would hold a thread indefinitely.

**Combined pipeline (in order of execution):**
```csharp
ResiliencePipelineBuilder()
    .AddRetry(...)          // Outer: retry with backoff
    .AddCircuitBreaker(...) // Middle: stop retrying if circuit is open
    .AddTimeout(...)        // Inner: each attempt has max 15s
```

**What to learn next:** Bulkhead isolation, hedging (parallel requests, take first success), Polly with `IHttpClientFactory`, chaos engineering.

---

### Response Compression

**What it is:** Compressing HTTP response bodies before sending to the client, reducing bandwidth and improving load times.

**Compression algorithms:**
- **Brotli** — newer, 15-25% better compression than Gzip, supported by all modern browsers
- **Gzip** — universal support, good compression

The client tells the server what it supports via the `Accept-Encoding: br, gzip` header. The server picks the best option.

**When to use it:**
- JSON APIs with large responses (book lists, recommendation results)
- Static text files (JavaScript, CSS)
- NOT for already-compressed content (images, videos)

**What to learn next:** HTTP/2 Server Push, CDN caching, content delivery optimization.

---

## 9. CQRS + MediatR

**CQRS = Command Query Responsibility Segregation**

**What it is:** Separating read operations (Queries) from write operations (Commands). Each operation is represented as an object and handled by a dedicated handler class.

**Why it exists:**
- Each handler has ONE responsibility (Single Responsibility Principle at the use-case level)
- Controllers are decoupled from business logic
- Easy to add cross-cutting behavior (logging, validation, caching) as pipeline behaviors
- Scales independently — read and write sides can use different databases

**The MediatR pattern:**
```
Controller → sends Request object → MediatR (message bus) → finds Handler → executes → returns response
```

Compare to traditional approach:
```csharp
// Traditional: Controller knows about service
public class BooksController : ControllerBase {
    private readonly IBookService _bookService; // tight coupling
    public async Task<IActionResult> GetAll()
        => Ok(await _bookService.GetAllBooksAsync());
}

// CQRS: Controller knows nothing about implementation
public class BooksCqrsController : ControllerBase {
    private readonly IMediator _mediator; // loose coupling
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetAllBooksQuery()));
}
```

**Request types:**
- **Query** — reads data, has no side effects
  - `GetAllBooksQuery → IReadOnlyList<BookDto>`
  - `GetBookByIdQuery(int id) → BookDto?`
  - `SearchBooksQuery(string term, ReadingStatus?) → IReadOnlyList<BookDto>`
- **Command** — changes state, returns result
  - `CreateBookCommand(Title, Author, ...) → BookDto`
  - `DeleteBookCommand(int id) → bool`

**Each handler is a single class:**
```csharp
public class GetAllBooksQueryHandler : IRequestHandler<GetAllBooksQuery, IReadOnlyList<BookDto>>
{
    private readonly IBookService _bookService;
    // Constructor injection...

    public async Task<IReadOnlyList<BookDto>> Handle(GetAllBooksQuery request, CancellationToken ct)
    {
        var books = await _bookService.GetAllBooksAsync();
        return books.Select(ToDto).ToList().AsReadOnly();
    }
}
```

**Both `/api/books` and `/api/books-cqrs` exist side by side** — this lets you compare the two approaches directly.

**What to learn next:** MediatR pipeline behaviors (like middleware for handlers), Event Sourcing, CQRS with separate read/write databases, Vertical Slice Architecture.

---

## 10. AI Integration

**What it does:** Given the user's reading history, asks Claude to recommend 5 books they'd enjoy, returning structured JSON.

**How it works:**

```
1. Collect user's books (read + currently reading)
2. Format as a text list
3. Send to Anthropic Claude API with a structured prompt
4. Parse the JSON response
5. Return BookRecommendationResult to the UI
```

**Prompt engineering:**
The prompt explicitly tells Claude:
- Respond ONLY in JSON (no markdown wrappers)
- Use a specific schema `{ reasoning, recommendations: [...] }`
- Don't recommend books already in the user's list

**Fallback handling:**
- No API key configured → returns helpful message, not an error
- Empty reading list → asks user to add books first
- API fails → Polly retries 3 times, then returns graceful error message

**Why structured output matters:**
JSON responses from LLMs need explicit instructions and post-processing (stripping markdown code fences) because models sometimes ignore "respond in JSON only." Always validate and sanitize LLM output.

**What to learn next:** Semantic Kernel (Microsoft's AI SDK), LangChain for .NET, function calling / tool use in LLMs, RAG (Retrieval-Augmented Generation), vector databases.

---

## 11. Background Services

**What it is:** A long-running task that runs alongside your web application.

**`LibraryStatsBackgroundService`:**
- Runs every N seconds (configured in `appsettings.json: LibraryStats:IntervalSeconds`)
- Queries the database for book counts by status
- Logs the statistics

```csharp
public class LibraryStatsBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Log stats
            await Task.Delay(_options.IntervalSeconds * 1000, stoppingToken);
        }
    }
}
```

**Why BackgroundService instead of a cron job?**
- Lives in the same process as your app — shares DI container
- Responds to app shutdown gracefully (CancellationToken)
- No external scheduler dependency

**What to learn next:** Hangfire (robust job scheduling with retries and UI), Quartz.NET, Azure Durable Functions, outbox pattern for reliable event processing.

---

## 12. Testing

### Unit Tests

**What they are:** Tests that verify ONE class in isolation, with all dependencies replaced by mocks.

**Why they matter:**
- Fast (milliseconds per test)
- Pinpoint failures to a specific class
- Run on every commit with zero infrastructure

**Tools used:**
- **xUnit** — test framework (attributes: `[Fact]`, `[Theory]`)
- **Moq** — mocking library (`Mock<IBookRepository>`, `.Setup(...)`, `.Verify(...)`)

**Example:**
```csharp
[Fact]
public async Task GetAllBooksAsync_ReturnsBooks()
{
    // Arrange
    var mockRepo = new Mock<IBookRepository>();
    mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Book> { new Book { Title = "Dune" } });

    var service = new BookService(mockRepo.Object, ...);

    // Act
    var result = await service.GetAllBooksAsync();

    // Assert
    Assert.Single(result);
    Assert.Equal("Dune", result.First().Title);
    mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
}
```

**47 unit tests cover:** BookBuilder, BookService, Specifications, Sort Strategies, Caching Decorator.

---

### Integration Tests

**What they are:** Tests that spin up the REAL application and make actual HTTP requests through the pipeline.

**Why they matter:**
- Test the whole stack: routing, auth, validation, middleware, database
- Catch integration bugs that unit tests miss
- Verify your API contract from the client's perspective

**What's different from unit tests:**
| Unit Test | Integration Test |
|-----------|-----------------|
| Tests ONE class | Tests the WHOLE app |
| Mocks dependencies | Uses real dependencies (or in-memory subs) |
| Milliseconds | Seconds |
| Finds logic bugs | Finds wiring, routing, auth bugs |

**`TestWebApplicationFactory`:**
```csharp
// Replaces real SQLite with in-memory database
services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
```

Each test run gets a UNIQUE in-memory database name (`Guid.NewGuid()`) — no data leaks between tests.

**9 integration tests cover:**
- Login with valid/invalid credentials
- `GET /api/books` with and without auth
- `POST /api/books` with valid/invalid data
- `GET /health`
- `GET /api/books/paged`
- `GET /api/books-cqrs`

**What to learn next:** Contract testing with PactNet, mutation testing with Stryker, performance testing with NBomber or k6.

---

## 13. Infrastructure

### Docker + docker-compose

**What Docker is:** Packages your app and all its dependencies into a portable container that runs identically everywhere.

**Why containers:**
- "Works on my machine" problem solved — the container IS the machine
- Consistent environments: dev → CI → staging → production
- Easy scaling and deployment

**Multi-stage Dockerfile:**
```dockerfile
# Stage 1: Build (has full SDK, large image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet restore && dotnet build ...

# Stage 2: Publish (built app, no SDK)
FROM build AS publish
RUN dotnet publish -o /app/publish

# Stage 3: Runtime (tiny image — no SDK, no build tools)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# Copy only the published files
COPY --from=publish /app/publish .
```

Multi-stage builds keep the final image small (runtime image is ~200MB vs SDK image ~700MB). The build tools never end up in production.

**Security:** Runs as non-root user (`appuser`) — a container best practice.

**docker-compose.yml:**
```yaml
services:
  booklibrary:
    build: .
    ports: ["8080:8080"]
    volumes:
      - booklibrary_data:/app/books.db  # persist SQLite across container restarts
    healthcheck:
      test: curl -f http://localhost:8080/health
```

**Named volumes** persist SQLite data when you `docker compose down` and `docker compose up`.

**What to learn next:** Docker networking, multi-container apps, container registries (Docker Hub, ECR, ACR), image scanning with Trivy.

---

### GitHub Actions CI/CD

**What it is:** Automated pipelines that run on every push to GitHub.

**Why it exists:**
- Catch broken builds before they reach production
- Run tests automatically — no "I forgot to run tests before pushing"
- Deploy automatically when tests pass (Continuous Deployment)

**CI pipeline (`.github/workflows/ci.yml`) — what it does:**
1. Trigger: push to `main` or any pull request
2. Checkout code
3. Setup .NET 8
4. `dotnet restore` — download NuGet packages
5. `dotnet build` — compile
6. `dotnet test` — run all tests
7. Deploy to Railway (if on `main`)

**The CI badge in README shows the status of the latest run.**

**What to learn next:** GitHub Actions matrix builds, artifact uploads, Docker builds in CI, OIDC-based deployments (no secrets), GitHub Environments for approvals.

---

### Railway Deployment

**What it is:** A PaaS (Platform-as-a-Service) similar to Heroku — deploy from Git, Railway handles the infrastructure.

**Why Railway over raw VMs:**
- No server management — Railway handles OS updates, networking, TLS
- Deploy with `git push` — no SSH, no Ansible
- Free tier available for hobby projects

**How deployment works:**
1. Railway connects to the GitHub repo
2. On push to `main`, Railway pulls the code
3. Detects .NET project (via `.csproj`) and builds it
4. Deploys the compiled app with `dotnet BookLibrary.dll`
5. Automatically provisions a TLS certificate

**Environment variables on Railway:**
- `Jwt__Key` — JWT signing secret
- `Anthropic__ApiKey` — AI recommendations key
- `ASPNETCORE_ENVIRONMENT=Production` — enables HSTS, disables Swagger UI

**What to learn next:** AWS ECS, Azure App Service, Google Cloud Run, Kubernetes, Terraform for infrastructure-as-code.

---

## 14. .NET Aspire (Coming Soon)

**.NET Aspire** is Microsoft's opinionated stack for building and running cloud-ready distributed applications locally.

**What it provides:**
- **Dashboard** — A local UI showing all your services, logs, metrics, and distributed traces side-by-side
- **Service Discovery** — Services find each other by name, not hardcoded URLs
- **Health Monitoring** — All services monitored from one place
- **Environment Variable Injection** — Configuration flows between services automatically
- **One-command startup** — `dotnet run` in the AppHost starts ALL services

**Think of it as:** Local Kubernetes, but simpler and .NET-first.

**To try Aspire:**
```bash
dotnet workload install aspire
```

Then the `BookLibrary.AppHost` project would orchestrate everything:
```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.BookLibrary>("booklibrary");
builder.Build().Run();
// Dashboard opens at https://localhost:15888
```

**What to learn next:** .NET Aspire with Redis, databases, message queues; Aspire to Kubernetes deployment.

---

## 15. Learning Path

This section maps out what to learn after mastering the concepts in this project. Organized by category.

---

### Cloud: AWS

| Service | What It Does | When To Use |
|---------|-------------|-------------|
| **EC2** | Virtual machines | Custom server configs, lift-and-shift migrations |
| **ECS / Fargate** | Container hosting (no server management) | Docker workloads without managing Kubernetes |
| **Lambda** | Serverless functions | Event-driven, short-lived tasks (image resize, webhooks) |
| **RDS** | Managed SQL databases (Postgres, MySQL, SQL Server) | Production databases without managing DB servers |
| **S3** | Object storage | File uploads, static website hosting, backups |
| **CloudFront** | CDN + edge caching | Static assets, API caching, global performance |
| **API Gateway** | Managed HTTP entry point | Rate limiting, auth, routing to Lambda |
| **SQS / SNS** | Message queues / pub-sub | Decoupled services, event-driven architecture |
| **Secrets Manager** | Secure secret storage | Connection strings, API keys (instead of env vars) |
| **CloudWatch** | Logs, metrics, alarms | Monitoring and alerting |
| **CodePipeline** | CI/CD pipelines | Automated build/deploy without GitHub Actions |

**Start with:** EC2 + RDS (familiar concepts), then ECS/Fargate (containers), then Lambda (serverless).

---

### Cloud: Azure

| Service | What It Does | When To Use |
|---------|-------------|-------------|
| **App Service** | PaaS web hosting | ASP.NET Core apps without Docker complexity |
| **Azure Container Apps** | Serverless containers | Like Fargate but with built-in Dapr support |
| **Azure SQL / CosmosDB** | SQL and NoSQL databases | Managed databases; CosmosDB for globally distributed |
| **Azure Functions** | Serverless (like Lambda) | Event-driven, triggers (HTTP, timer, queue) |
| **Service Bus** | Enterprise message broker | Reliable messaging between services |
| **Blob Storage** | Object storage (like S3) | Files, backups, static assets |
| **Key Vault** | Secrets management | API keys, connection strings, certificates |
| **Application Insights** | APM and monitoring | Distributed tracing, live metrics, failure analysis |
| **Azure DevOps / GitHub Actions** | CI/CD | Automated pipelines |

**If you know AWS:** App Service ≈ Elastic Beanstalk, Service Bus ≈ SQS/SNS, CosmosDB ≈ DynamoDB.

---

### Kubernetes

**What it is:** Container orchestration — runs, scales, and manages containers across a cluster of machines.

**Key concepts to learn (in order):**
1. **Pods** — one or more containers running together (the smallest deployable unit)
2. **Deployments** — declarative: "I want 3 replicas of this pod"
3. **Services** — stable network endpoint for pods (load balancing)
4. **ConfigMaps + Secrets** — inject config into pods
5. **Ingress** — HTTP routing into the cluster
6. **Namespaces** — logical isolation within a cluster
7. **HPA (Horizontal Pod Autoscaler)** — auto-scale pods based on CPU/memory
8. **Helm Charts** — package manager for Kubernetes apps

**Kubernetes readiness/liveness probes** use your `/health` endpoint:
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  periodSeconds: 30
readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 10
```

**Recommended resources:** Kubernetes official documentation, "Kubernetes in Action" by Marko Lukša, Play with Kubernetes (pwk.labs.play-with-k8s.com).

---

### Docker Deep Dive

Beyond the basics covered in this project:

- **Docker networking** — bridge, host, overlay networks; how containers communicate
- **Docker volumes** — bind mounts vs named volumes vs tmpfs
- **Multi-container apps** — compose v3, health check dependencies, service ordering
- **Image optimization** — minimizing layers, `.dockerignore`, distroless images
- **Container registries** — Docker Hub, AWS ECR, Azure ACR, GitHub Container Registry
- **Image scanning** — Trivy, Snyk, Grype for vulnerability scanning
- **Multi-arch builds** — `docker buildx` for ARM64 + AMD64 (M1 Macs, AWS Graviton)

---

### gRPC

**What it is:** A high-performance, binary RPC framework by Google. Alternative to REST for service-to-service communication.

**Why choose gRPC over REST:**
- 7-10x faster serialization (Protocol Buffers vs JSON)
- Strongly-typed contracts via `.proto` files
- Bidirectional streaming
- Auto-generated client code in any language

**When to use:**
- Internal microservice communication (not public APIs)
- High-throughput data pipelines
- Real-time streaming (server-to-client, client-to-server)

**In .NET:**
```protobuf
service BookService {
  rpc GetBook (GetBookRequest) returns (BookReply);
  rpc ListBooks (Empty) returns (stream BookReply);
}
```
`dotnet-grpc` generates C# client and server stubs from the `.proto` file.

**What to learn next:** Protobuf encoding, gRPC-Web for browser clients, gRPC with ASP.NET Core.

---

### Event-Driven Architecture

**What it is:** Services communicate by publishing and consuming events, rather than calling each other directly (REST/gRPC).

**Why it matters:**
- **Decoupling** — the publisher doesn't know or care who consumes the event
- **Resilience** — if a consumer is down, messages queue up (no data loss)
- **Scalability** — consumers can be scaled independently
- **Auditability** — event log is a natural audit trail

**Key technologies:**

| Technology | Type | Best For |
|------------|------|----------|
| **RabbitMQ** | Message broker | General-purpose messaging, routing patterns |
| **Azure Service Bus** | Managed broker | Enterprise .NET applications in Azure |
| **Apache Kafka** | Event streaming | High-throughput event logs, replay semantics |
| **AWS SQS/SNS** | Managed queues/topics | AWS-native event-driven apps |
| **MassTransit** | .NET abstraction | Unifies RabbitMQ, SQS, Service Bus behind one API |

**Patterns to learn:**
- **Publish/Subscribe** — one publisher, many consumers
- **Competing Consumers** — multiple instances of one consumer, each processes one message
- **Saga Pattern** — long-running business process across multiple services
- **Outbox Pattern** — atomic DB write + event publish (no message loss)
- **Event Sourcing** — store events as the source of truth, not state

---

### Microservices Patterns

Once you're building distributed systems:

| Pattern | Problem It Solves |
|---------|-------------------|
| **API Gateway** | Single entry point, auth, rate limiting, routing |
| **Circuit Breaker** | Stop calling a failing service (Polly — already in this project!) |
| **Service Discovery** | Find service addresses dynamically (Consul, Eureka, Kubernetes DNS) |
| **Distributed Tracing** | Follow a request across 10 services (OpenTelemetry — already here!) |
| **Distributed Caching** | Share cache across instances (Redis) |
| **Distributed Locking** | Prevent race conditions across instances (Redlock) |
| **Bulkhead** | Isolate failures (separate thread pools per external call) |
| **Strangler Fig** | Gradually replace a monolith with microservices |
| **CQRS + Event Sourcing** | Scale reads and writes independently |

**Rule of thumb:** Don't start with microservices. Build a well-structured monolith first (like this app). Extract services only when you have a clear scalability or team boundary reason.

---

### Recommended Books

**Foundations:**
- *Clean Code* — Robert C. Martin — how to write readable, maintainable code
- *Clean Architecture* — Robert C. Martin — layering and dependency rules
- *Domain-Driven Design* — Eric Evans — modeling complex business domains
- *Designing Data-Intensive Applications* — Martin Kleppmann — databases, distributed systems

**Patterns:**
- *Design Patterns: Elements of Reusable Object-Oriented Software* — Gang of Four — the classic patterns reference
- *Patterns of Enterprise Application Architecture* — Martin Fowler — Repository, Unit of Work, etc.
- *Microservices Patterns* — Chris Richardson — CQRS, Saga, event sourcing, and more

**.NET Specific:**
- *Pro ASP.NET Core 7* — Adam Freeman — comprehensive ASP.NET Core reference
- *C# in Depth* — Jon Skeet — deep dive into C# language features
- *Concurrency in C#* — Stephen Cleary — async/await, Channels, reactive programming

**Cloud & DevOps:**
- *Kubernetes in Action* — Marko Lukša — best K8s book
- *The DevOps Handbook* — Kim, Humble, Debois — culture and practices
- *Site Reliability Engineering* — Google — production operations at scale

---

### Recommended Courses

- **Microsoft Learn** (free) — ASP.NET Core, Azure, EF Core learning paths
- **Pluralsight** — .NET, cloud, architecture courses (paid, worth it)
- **Udemy** — Kubernetes for developers, Docker Mastery (affordable)
- **freeCodeCamp YouTube** — good free intros to Docker, Kubernetes, AWS
- **NDC Conference talks** — YouTube, free — industry experts on architecture topics

---

*This document is a living reference. As the project evolves, new patterns and features will be added here.*
