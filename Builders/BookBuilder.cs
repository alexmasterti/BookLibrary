using BookLibrary.Models;

namespace BookLibrary.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="Book"/> objects step by step.
///
/// PATTERN: Builder
///   The Builder pattern constructs a complex object incrementally.
///   Each method configures one part of the object and returns <c>this</c>
///   so calls can be chained (the "fluent" style).
///
///   Usage:
///   <code>
///   var book = new BookBuilder()
///       .WithTitle("Clean Code")
///       .WithAuthor("Robert C. Martin")
///       .WithGenre("Programming")
///       .WithYear(2008)
///       .WithStatus(ReadingStatus.WantToRead)
///       .Build();
///   </code>
///
///   Why use Builder instead of just setting properties directly?
///   - Readability: the fluent chain reads like a sentence.
///   - Validation: Build() is the single point where rules are enforced
///     before the object leaves the builder.
///   - Extensibility: adding a new optional field means adding one method
///     here; no constructor overloads needed.
///
/// NOTE: BookFactory uses BookBuilder internally. Factory decides WHAT
/// to create; Builder decides HOW to assemble it. The two patterns
/// compose naturally.
/// </summary>
public class BookBuilder
{
    // The book being assembled. Each With* method mutates it and
    // returns 'this' to enable method chaining.
    private readonly Book _book = new();

    /// <summary>Sets the title. Required — Build() throws if omitted.</summary>
    public BookBuilder WithTitle(string title)
    {
        _book.Title = title;
        return this;
    }

    /// <summary>Sets the author. Required — Build() throws if omitted.</summary>
    public BookBuilder WithAuthor(string author)
    {
        _book.Author = author;
        return this;
    }

    /// <summary>Sets the genre (optional).</summary>
    public BookBuilder WithGenre(string? genre)
    {
        _book.Genre = genre;
        return this;
    }

    /// <summary>Sets the publication year (optional).</summary>
    public BookBuilder WithYear(int? year)
    {
        _book.Year = year;
        return this;
    }

    /// <summary>Sets the reading status. Defaults to WantToRead if not called.</summary>
    public BookBuilder WithStatus(ReadingStatus status)
    {
        _book.Status = status;
        return this;
    }

    /// <summary>
    /// Validates all required fields and returns the fully constructed
    /// <see cref="Book"/>. Throws <see cref="InvalidOperationException"/>
    /// if Title or Author are missing.
    ///
    /// Having a dedicated Build() step means invalid objects can never
    /// leave the builder — the contract is enforced in one place.
    /// </summary>
    public Book Build()
    {
        if (string.IsNullOrWhiteSpace(_book.Title))
            throw new InvalidOperationException("Book Title is required.");

        if (string.IsNullOrWhiteSpace(_book.Author))
            throw new InvalidOperationException("Book Author is required.");

        return _book;
    }
}
