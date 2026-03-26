using BookLibrary.DTOs.Book;
using BookLibrary.Validators.Book;

namespace BookLibrary.Tests;

public class ValidatorTests
{
    private readonly CreateBookValidator _createValidator = new();
    private readonly UpdateBookValidator _updateValidator = new();

    // ── CreateBookValidator ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateValidator_ValidRequest_Passes()
    {
        var request = new CreateBookRequest("Clean Code", "Robert Martin", "Programming", 2008, "Read");
        var result  = await _createValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_EmptyTitle_Fails()
    {
        var request = new CreateBookRequest("", "Author", null, null, null);
        var result  = await _createValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task CreateValidator_EmptyAuthor_Fails()
    {
        var request = new CreateBookRequest("Title", "", null, null, null);
        var result  = await _createValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Author");
    }

    [Fact]
    public async Task CreateValidator_TitleTooLong_Fails()
    {
        var request = new CreateBookRequest(new string('A', 201), "Author", null, null, null);
        var result  = await _createValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_YearTooOld_Fails()
    {
        var request = new CreateBookRequest("Title", "Author", null, 999, null);
        var result  = await _createValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Year");
    }

    [Fact]
    public async Task CreateValidator_FutureYear_Fails()
    {
        var request = new CreateBookRequest("Title", "Author", null, DateTime.UtcNow.Year + 1, null);
        var result  = await _createValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_NullYear_Passes()
    {
        var request = new CreateBookRequest("Title", "Author", null, null, null);
        var result  = await _createValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_InvalidStatus_Fails()
    {
        var request = new CreateBookRequest("Title", "Author", null, null, "NotAStatus");
        var result  = await _createValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Status");
    }

    // ── UpdateBookValidator ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateValidator_ValidRequest_Passes()
    {
        var request = new UpdateBookRequest("Clean Code", "Robert Martin", null, null, "Read");
        var result  = await _updateValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateValidator_EmptyStatus_Fails()
    {
        var request = new UpdateBookRequest("Title", "Author", null, null, "");
        var result  = await _updateValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Status");
    }
}
