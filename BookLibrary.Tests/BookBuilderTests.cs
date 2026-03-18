using BookLibrary.Builders;
using BookLibrary.Models;

namespace BookLibrary.Tests;

public class BookBuilderTests
{
    [Fact]
    public void Build_WithValidData_ReturnsBook()
    {
        var book = new BookBuilder()
            .WithTitle("Clean Code")
            .WithAuthor("Robert C. Martin")
            .WithGenre("Programming")
            .WithYear(2008)
            .Build();

        Assert.Equal("Clean Code", book.Title);
        Assert.Equal("Robert C. Martin", book.Author);
        Assert.Equal("Programming", book.Genre);
        Assert.Equal(2008, book.Year);
        Assert.Equal(ReadingStatus.WantToRead, book.Status);
    }

    [Fact]
    public void Build_WithStatus_SetsStatus()
    {
        var book = new BookBuilder()
            .WithTitle("The Pragmatic Programmer")
            .WithAuthor("David Thomas")
            .WithStatus(ReadingStatus.Read)
            .Build();

        Assert.Equal(ReadingStatus.Read, book.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WithEmptyTitle_ThrowsException(string title)
    {
        var builder = new BookBuilder().WithTitle(title).WithAuthor("Author");
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WithEmptyAuthor_ThrowsException(string author)
    {
        var builder = new BookBuilder().WithTitle("Title").WithAuthor(author);
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Theory]
    [InlineData(ReadingStatus.WantToRead)]
    [InlineData(ReadingStatus.CurrentlyReading)]
    [InlineData(ReadingStatus.Read)]
    public void Build_WithEachStatus_Succeeds(ReadingStatus status)
    {
        var book = new BookBuilder()
            .WithTitle("Title")
            .WithAuthor("Author")
            .WithStatus(status)
            .Build();

        Assert.Equal(status, book.Status);
    }
}
