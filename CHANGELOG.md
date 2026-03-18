# Changelog

All notable changes to BookLibrary are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [2.0.0] — 2026-03-18

### Added — Infrastructure
- **Serilog** structured logging (`Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Enrichers.Environment`)
  - Rolling daily log files in `logs/booklibrary-YYYY-MM-DD.log` (7-day retention)
  - Single-line structured request logs: `GET /api/books responded 200 in 12.3ms`
  - Machine name + environment name enrichment for multi-server setups
- **Docker** support (`Dockerfile`, `.dockerignore`, `docker-compose.yml`)
  - Multi-stage build: restore → build → test → publish → runtime
  - Non-root `appuser` for security
  - `docker-compose.yml` with health check and named volume for SQLite persistence
- **Response Compression** with Brotli + Gzip
  - Brotli (fastest level) preferred by modern browsers
  - Gzip (smallest size) fallback for older clients
  - Enabled for HTTPS responses
- **Scalar API Reference** — modern, beautiful replacement for Swagger UI
  - Accessible at `/scalar/v1` in Development mode
  - DeepSpace dark theme
- **CI badge** in README.md pointing to GitHub Actions workflow

### Added — Resilience & API Versioning
- **Polly Resilience Pipeline** in `BookRecommendationService`
  - Exponential backoff retry (3 attempts, 2s/4s/8s delays)
  - Circuit breaker (opens after 50% failure rate over 30s window, min 3 calls)
  - 15-second timeout per attempt
- **API Versioning** (`Asp.Versioning.Http`, `Asp.Versioning.Mvc.ApiExplorer`)
  - Version readers: URL segment (`/api/v2/books`), header (`X-API-Version`), query (`?api-version=2.0`)
  - `BooksController` annotated as `v1.0`
  - New `BooksV2Controller` with enriched `BookDtoV2` (computed fields: `DaysInLibrary`, `IsRecentlyAdded`, `Era`)

### Added — Advanced Patterns
- **MediatR + CQRS** (`MediatR` 14.x)
  - Commands: `CreateBookCommand`, `DeleteBookCommand`
  - Queries: `GetAllBooksQuery`, `GetBookByIdQuery`, `SearchBooksQuery`
  - Handlers: one class per operation, single responsibility at use-case level
  - New `BooksCqrsController` at `/api/books-cqrs` for comparison with traditional approach
- **Integration Tests** (`Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`)
  - `TestWebApplicationFactory` — spins up the full app pipeline with in-memory SQLite
  - 9 integration tests covering auth, books CRUD, health checks, pagination, CQRS endpoint
- **OpenTelemetry** (`OpenTelemetry.Extensions.Hosting`, `.Instrumentation.AspNetCore`, `.Instrumentation.Http`, `.Exporter.Console`)
  - Traces: full request lifecycle with exception recording
  - Metrics: request counts, durations via ASP.NET Core instrumentation
  - Health check paths filtered from traces (too noisy)
  - `BookLibraryTelemetry` static helper for custom spans
- **ARCHITECTURE.md** — comprehensive learning guide rewritten from scratch
  - Junior-dev-friendly explanations of every pattern and feature
  - Learning Path section covering AWS, Azure, Kubernetes, Docker, gRPC, event-driven architecture

---

## [1.0.0] — 2026-03-01

### Added — Initial release
- **Blazor Server** UI with dark/light theme toggle, toast notifications, confirm dialogs
- **REST API** with JWT Bearer authentication
- **Repository Pattern** — generic `IRepository<T>` + `IBookRepository`
- **Decorator Pattern** — `CachingBookRepository` → `LoggingBookRepository` → `BookRepository`
- **Strategy Pattern** — pluggable sort algorithms (Title, Author, Year)
- **Specification Pattern** — composable filter rules (`AndSpecification`, `StatusSpecification`, `TitleOrAuthorContainsSpecification`)
- **Factory + Builder Pattern** — `BookFactory` + `BookBuilder` for validated object creation
- **Options Pattern** — typed config for JWT, Anthropic, Cache, LibraryStats
- **FluentValidation** — `CreateBookValidator`, `UpdateBookValidator` with auto-wired middleware
- **JWT Authentication** — stateless signed tokens, configurable expiry
- **Rate Limiting** — fixed window: 100 req/min (API), 10 req/min (auth)
- **Health Checks** — `/health` and `/health/detail` with DB check
- **Global Exception Handler** — RFC 7807 ProblemDetails for unhandled exceptions
- **Pagination** — `GET /api/books/paged` with `PaginatedResult<T>`
- **AI Book Recommendations** — Anthropic Claude API integration with structured JSON parsing
- **Background Service** — `LibraryStatsBackgroundService` for periodic stats logging
- **In-Memory Caching** — cache-aside strategy with write-through invalidation
- **GitHub Actions CI/CD** — build, test, and Railway deploy on push to `main`
- **47 Unit Tests** — xUnit + Moq covering all layers
