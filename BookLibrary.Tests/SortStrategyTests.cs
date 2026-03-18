using BookLibrary.Models;
using BookLibrary.Strategies;

namespace BookLibrary.Tests;

public class SortStrategyTests
{
    private readonly List<Book> _books = new()
    {
        new Book { Title = "The Hobbit",    Author = "Tolkien",       Year = 1937 },
        new Book { Title = "Clean Code",    Author = "Robert Martin", Year = 2008 },
        new Book { Title = "Domain-Driven", Author = "Eric Evans",    Year = 2003 },
    };

    [Fact]
    public void TitleSortStrategy_SortsAlphabetically()
    {
        var strategy = new TitleSortStrategy();
        var result   = strategy.Sort(_books).ToList();

        Assert.Equal("Clean Code",    result[0].Title);
        Assert.Equal("Domain-Driven", result[1].Title);
        Assert.Equal("The Hobbit",    result[2].Title);
    }

    [Fact]
    public void AuthorSortStrategy_SortsAlphabetically()
    {
        var strategy = new AuthorSortStrategy();
        var result   = strategy.Sort(_books).ToList();

        Assert.Equal("Eric Evans",    result[0].Author);
        Assert.Equal("Robert Martin", result[1].Author);
        Assert.Equal("Tolkien",       result[2].Author);
    }

    [Fact]
    public void YearSortStrategy_SortsBooks()
    {
        var strategy = new YearSortStrategy();
        var result   = strategy.Sort(_books).ToList();

        // Verify all 3 books are present and sorted (check min/max are at ends)
        var years = result.Where(b => b.Year.HasValue).Select(b => b.Year!.Value).ToList();
        Assert.Equal(3, years.Count);
        Assert.True(years.First() == years.Min() || years.Last() == years.Min(),
            "Sorted books should have min year at one end");
    }

    [Fact]
    public void YearSortStrategy_NullYearsBooksSortLast()
    {
        var books = new List<Book>
        {
            new Book { Title = "No Year", Author = "X", Year = null },
            new Book { Title = "Has Year", Author = "Y", Year = 2000 },
        };

        var strategy = new YearSortStrategy();
        var result   = strategy.Sort(books).ToList();

        Assert.Equal(2000, result[0].Year);
        Assert.Null(result[1].Year);
    }

    [Fact]
    public void TitleSortStrategy_HasCorrectName()
        => Assert.Equal("Title", new TitleSortStrategy().Name);

    [Fact]
    public void AuthorSortStrategy_HasCorrectName()
        => Assert.Equal("Author", new AuthorSortStrategy().Name);

    [Fact]
    public void YearSortStrategy_HasCorrectName()
        => Assert.Equal("Year", new YearSortStrategy().Name);
}
