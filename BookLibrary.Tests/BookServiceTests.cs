using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Services;
using BookLibrary.Strategies;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BookLibrary.Tests;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _repoMock;
    private readonly BookService _service;

    private readonly List<Book> _books = new()
    {
        new Book { Id = 1, Title = "Clean Code",          Author = "Robert Martin", Genre = "Programming", Year = 2008, Status = ReadingStatus.Read },
        new Book { Id = 2, Title = "The Hobbit",           Author = "Tolkien",       Genre = "Fantasy",     Year = 1937, Status = ReadingStatus.CurrentlyReading },
        new Book { Id = 3, Title = "Pragmatic Programmer", Author = "David Thomas",  Genre = "Programming", Year = 1999, Status = ReadingStatus.WantToRead },
    };

    public BookServiceTests()
    {
        _repoMock = new Mock<IBookRepository>();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_books);

        var strategies = new List<ISortStrategy<Book>>
        {
            new TitleSortStrategy(),
            new AuthorSortStrategy(),
            new YearSortStrategy()
        };

        _service = new BookService(_repoMock.Object, strategies);
    }

    [Fact]
    public async Task GetAllBooksAsync_ReturnsAllBooks()
    {
        var result = await _service.GetAllBooksAsync();

        Assert.Equal(3, result.Count);
        _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithTextQuery_FiltersCorrectly()
    {
        var result = await _service.SearchAsync("Clean", null);

        Assert.Single(result);
        Assert.Equal("Clean Code", result[0].Title);
    }

    [Fact]
    public async Task SearchAsync_WithStatus_FiltersCorrectly()
    {
        var result = await _service.SearchAsync(string.Empty, ReadingStatus.Read);

        Assert.Single(result);
        Assert.Equal(ReadingStatus.Read, result[0].Status);
    }

    [Fact]
    public async Task SearchAsync_WithTextAndStatus_AppliesBothFilters()
    {
        var result = await _service.SearchAsync("Martin", ReadingStatus.Read);

        Assert.Single(result);
        Assert.Equal("Clean Code", result[0].Title);
    }

    [Fact]
    public async Task SearchAsync_WithTitleSort_ReturnsSortedByTitle()
    {
        var result = await _service.SearchAsync(string.Empty, null, "Title");

        Assert.Equal("Clean Code",          result[0].Title);
        Assert.Equal("Pragmatic Programmer", result[1].Title);
        Assert.Equal("The Hobbit",           result[2].Title);
    }

    [Fact]
    public async Task SearchAsync_WithYearSort_ReturnsSortedBooks()
    {
        var result = await _service.SearchAsync(string.Empty, null, "Year");

        // All books returned, sorted by year (direction depends on strategy implementation)
        Assert.Equal(3, result.Count);
        var years = result.Select(b => b.Year ?? 0).ToList();
        var sorted = years.OrderBy(y => y).ToList();
        var sortedDesc = years.OrderByDescending(y => y).ToList();
        Assert.True(years.SequenceEqual(sorted) || years.SequenceEqual(sortedDesc),
            "Books should be sorted by year either ascending or descending");
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        var request = new DTOs.PagedBooksRequest { PageNumber = 1, PageSize = 2 };
        var result  = await _service.GetPagedAsync(request);

        Assert.Equal(2, result.Items.Count());
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainingItems()
    {
        var request = new DTOs.PagedBooksRequest { PageNumber = 2, PageSize = 2 };
        var result  = await _service.GetPagedAsync(request);

        Assert.Single(result.Items);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task AddBookAsync_DelegatesToRepository()
    {
        var book = new Book { Title = "New Book", Author = "Author" };
        await _service.AddBookAsync(book);
        _repoMock.Verify(r => r.AddAsync(book), Times.Once);
    }

    [Fact]
    public async Task DeleteBookAsync_DelegatesToRepository()
    {
        await _service.DeleteBookAsync(1);
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
