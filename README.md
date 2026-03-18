# 📚 BookLibrary

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![SQLite](https://img.shields.io/badge/SQLite-EF%20Core-003B57?logo=sqlite)
![License](https://img.shields.io/badge/license-MIT-green)
[![CI](https://github.com/alexmasterti/BookLibrary/actions/workflows/ci.yml/badge.svg)](https://github.com/alexmasterti/BookLibrary/actions/workflows/ci.yml)

A full-stack **Blazor Server** application with a **REST API** backend, built to demonstrate enterprise-grade software engineering patterns and modern .NET 8 practices — including **AI-powered book recommendations** via the Anthropic Claude API.

---

## ✨ Features

### Architecture & Patterns
- **Repository Pattern** — Generic + Book-specific repositories
- **Decorator Pattern** — Caching → Logging → Repository chain
- **Strategy Pattern** — Pluggable sort algorithms (Title, Author, Year)
- **Specification Pattern** — Composable, reusable filter rules
- **Factory + Builder Pattern** — Centralized, validated object creation
- **Options Pattern** — Strongly-typed configuration from appsettings.json
- **SOLID Principles** — Applied throughout every layer

### API & Security
- **JWT Bearer Authentication** — Stateless, signed tokens
- **Swagger/OpenAPI** — Interactive API docs at `/swagger`
- **Rate Limiting** — 100 req/min (API), 10 req/min (auth endpoint)
- **Global Exception Handling** — RFC 7807 ProblemDetails responses
- **FluentValidation** — Request validation with rich error messages

### Infrastructure
- **Health Checks** — `/health` and `/health/detail` endpoints
- **Custom Middleware** — Request timing logger
- **Background Service** — Periodic library stats logger
- **In-Memory Caching** — Cache-aside strategy with write invalidation
- **Pagination** — `GET /api/books/paged` with full metadata

### UI
- **Blazor Server** — Real-time, component-based UI
- **Dark/Light Theme** — Persisted via localStorage
- **Toast Notifications** — Reusable success/error/info component
- **Confirm Dialog** — Reusable modal with EventCallback pattern

### AI
- **AI Book Recommendations** — Claude-powered suggestions based on your reading history

### Testing
- **42 Unit Tests** — xUnit + Moq, covering all layers

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### Run locally

```bash
git clone https://github.com/alexmasterti/BookLibrary.git
cd BookLibrary
dotnet run
```

Open your browser at: **https://localhost:7xxx** (port shown in terminal)

### Optional: AI Recommendations
Add your Anthropic API key to `appsettings.json`:
```json
"Anthropic": {
  "ApiKey": "sk-ant-...",
  "Model": "claude-3-5-haiku-20241022"
}
```
Or set via environment variable: `Anthropic__ApiKey=sk-ant-...`

---

## 🔌 REST API

### Authentication

```bash
# Get a JWT token
POST /api/auth/login
{
  "username": "admin",
  "password": "BookShelf2024!"
}
# Response: { "token": "eyJ...", "expiresAt": "..." }
```

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | ❌ | Get JWT token |
| GET | `/api/books` | ✅ | Get all books |
| GET | `/api/books/{id}` | ✅ | Get book by ID |
| GET | `/api/books/search` | ✅ | Search with filters |
| GET | `/api/books/paged` | ✅ | Paginated list |
| POST | `/api/books` | ✅ | Create book |
| PUT | `/api/books/{id}` | ✅ | Update book |
| DELETE | `/api/books/{id}` | ✅ | Delete book |
| GET | `/api/recommendations` | ✅ | AI recommendations |
| GET | `/health` | ❌ | Basic health check |
| GET | `/health/detail` | ❌ | Detailed health JSON |

### Swagger UI
Available at `/swagger` in Development mode. Click **Authorize**, enter `Bearer <your-token>`.

---

## 🧪 Tests

```bash
dotnet test
```

42 tests covering: BookBuilder, BookService, Specifications, Sort Strategies, Caching Decorator.

---

## 🏗️ Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for a detailed breakdown of every pattern, principle, and design decision.

---

## 📁 Project Structure

```
BookLibrary/
├── Controllers/        # REST API (BooksController, AuthController, RecommendationsController)
├── DTOs/               # Request/response shapes (never expose domain models directly)
├── Validators/         # FluentValidation rules
├── Services/           # Business logic (BookService, TokenService, BookRecommendationService)
├── Repositories/       # Data access + Decorator chain
├── Interfaces/         # All abstractions
├── Models/             # Domain models (Book, BaseEntity, ReadingStatus)
├── Specifications/     # Composable filter rules
├── Strategies/         # Sort algorithms
├── Factories/          # Object creation
├── Builders/           # Fluent builder
├── Options/            # Typed config classes
├── Middleware/         # RequestTiming, GlobalExceptionHandler
├── BackgroundServices/ # LibraryStatsBackgroundService
├── Pages/              # Blazor pages (Index, Books, BookForm, Recommendations)
├── Shared/             # Layout, NavMenu, Toast, ConfirmDialog
└── Program.cs          # Composition root — all DI wiring
```
