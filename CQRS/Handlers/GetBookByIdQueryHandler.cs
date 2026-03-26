using BookLibrary.CQRS.Queries;
using BookLibrary.DTOs.Book;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using MediatR;

namespace BookLibrary.CQRS.Handlers;

public class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, BookDto?>
{
    private readonly IBookService _bookService;
    public GetBookByIdQueryHandler(IBookService bookService) => _bookService = bookService;

    public async Task<BookDto?> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        var book = await _bookService.GetBookByIdAsync(request.Id);
        if (book is null) return null;
        return new BookDto(book.Id, book.Title, book.Author, book.Genre, book.Year, book.Status.ToString(), book.CreatedAt);
    }
}
