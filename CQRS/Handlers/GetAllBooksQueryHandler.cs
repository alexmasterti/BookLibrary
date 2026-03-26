using BookLibrary.CQRS.Queries;
using BookLibrary.DTOs.Book;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using MediatR;

namespace BookLibrary.CQRS.Handlers;

/// <summary>
/// Handler for GetAllBooksQuery.
/// Each handler has ONE job: handle ONE query or command.
/// This is Single Responsibility at the use-case level.
/// </summary>
public class GetAllBooksQueryHandler : IRequestHandler<GetAllBooksQuery, IReadOnlyList<BookDto>>
{
    private readonly IBookService _bookService;

    public GetAllBooksQueryHandler(IBookService bookService)
        => _bookService = bookService;

    public async Task<IReadOnlyList<BookDto>> Handle(GetAllBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await _bookService.GetAllBooksAsync();
        return books.Select(b => new BookDto(b.Id, b.Title, b.Author, b.Genre, b.Year, b.Status.ToString(), b.CreatedAt))
                    .ToList()
                    .AsReadOnly();
    }
}
