using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookLibrary.DTOs.Auth;
using BookLibrary.DTOs.Book;
using BookLibrary.DTOs.Common;
using Xunit;

namespace BookLibrary.Tests.Integration;

/// <summary>
/// Integration tests — test the REAL HTTP pipeline end-to-end.
/// Difference from unit tests:
///   Unit tests: test one class in isolation with mocks
///   Integration tests: test the whole app (routing, auth, validation, DB) together
/// </summary>
public class BooksApiIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BooksApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "BookShelf2024!" });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "BookShelf2024!" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result?.Token);
        Assert.NotEmpty(result!.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "wrong", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBooks_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/books");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBooks_WithAuth_Returns200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/books");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateBook_ValidData_ReturnsBook()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/books",
            new { title = "Integration Test Book", author = "Test Author", genre = "Testing", year = 2024, status = "WantToRead" });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateBook_EmptyTitle_Returns400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/books",
            new { title = "", author = "Author", genre = (string?)null, year = (int?)null, status = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetPagedBooks_WithAuth_ReturnsPaginatedResult()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/books/paged?pageNumber=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<BookDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.PageNumber);
    }

    [Fact]
    public async Task CqrsGetBooks_WithAuth_Returns200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/books-cqrs");
        response.EnsureSuccessStatusCode();
    }
}
