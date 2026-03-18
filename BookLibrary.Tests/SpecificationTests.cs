using BookLibrary.Models;
using BookLibrary.Specifications;

namespace BookLibrary.Tests;

public class SpecificationTests
{
    private readonly Book _programmingBook = new()
    {
        Title  = "Clean Code",
        Author = "Robert Martin",
        Status = ReadingStatus.Read
    };

    private readonly Book _fantasyBook = new()
    {
        Title  = "The Hobbit",
        Author = "Tolkien",
        Status = ReadingStatus.CurrentlyReading
    };

    // ── TitleOrAuthorContains ─────────────────────────────────────────────────

    [Theory]
    [InlineData("clean")]
    [InlineData("CLEAN")]
    [InlineData("Clean Code")]
    [InlineData("Robert")]
    [InlineData("martin")]
    public void TitleOrAuthorContains_CaseInsensitive_Matches(string query)
    {
        var spec = new TitleOrAuthorContainsSpecification(query);
        Assert.True(spec.IsSatisfiedBy(_programmingBook));
    }

    [Fact]
    public void TitleOrAuthorContains_NoMatch_ReturnsFalse()
    {
        var spec = new TitleOrAuthorContainsSpecification("Tolkien");
        Assert.False(spec.IsSatisfiedBy(_programmingBook));
    }

    // ── StatusSpecification ───────────────────────────────────────────────────

    [Fact]
    public void StatusSpec_MatchingStatus_ReturnsTrue()
    {
        var spec = new StatusSpecification(ReadingStatus.Read);
        Assert.True(spec.IsSatisfiedBy(_programmingBook));
    }

    [Fact]
    public void StatusSpec_NonMatchingStatus_ReturnsFalse()
    {
        var spec = new StatusSpecification(ReadingStatus.WantToRead);
        Assert.False(spec.IsSatisfiedBy(_programmingBook));
    }

    // ── AndSpecification ──────────────────────────────────────────────────────

    [Fact]
    public void AndSpec_BothSatisfied_ReturnsTrue()
    {
        var textSpec   = new TitleOrAuthorContainsSpecification("Robert");
        var statusSpec = new StatusSpecification(ReadingStatus.Read);
        var and        = new AndSpecification<Book>(textSpec, statusSpec);

        Assert.True(and.IsSatisfiedBy(_programmingBook));
    }

    [Fact]
    public void AndSpec_OneFails_ReturnsFalse()
    {
        var textSpec   = new TitleOrAuthorContainsSpecification("Robert");
        var statusSpec = new StatusSpecification(ReadingStatus.WantToRead); // doesn't match
        var and        = new AndSpecification<Book>(textSpec, statusSpec);

        Assert.False(and.IsSatisfiedBy(_programmingBook));
    }

    [Fact]
    public void AndSpec_CanBeNested()
    {
        var textSpec   = new TitleOrAuthorContainsSpecification("Clean");
        var statusSpec = new StatusSpecification(ReadingStatus.Read);
        var authorSpec = new TitleOrAuthorContainsSpecification("Martin");

        var nested = new AndSpecification<Book>(
            textSpec,
            new AndSpecification<Book>(statusSpec, authorSpec));

        Assert.True(nested.IsSatisfiedBy(_programmingBook));
        Assert.False(nested.IsSatisfiedBy(_fantasyBook));
    }
}
