using BookLibrary.CQRS.Commands;
using BookLibrary.DTOs;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using MediatR;

namespace BookLibrary.CQRS.Handlers;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, BookDto>
{
    private readonly IBookService _bookService;
    private readonly IBookFactory _bookFactory;

    public CreateBookCommandHandler(IBookService bookService, IBookFactory bookFactory)
    {
        _bookService = bookService;
        _bookFactory = bookFactory;
    }

    public async Task<BookDto> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        ReadingStatus status = ReadingStatus.WantToRead;
        if (request.Status is not null)
            Enum.TryParse(request.Status, true, out status);

        var book = _bookFactory.Create(request.Title, request.Author, request.Genre, request.Year, status);
        await _bookService.AddBookAsync(book);
        return new BookDto(book.Id, book.Title, book.Author, book.Genre, book.Year, book.Status.ToString(), book.CreatedAt);
    }
}
