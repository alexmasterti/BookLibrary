using BookLibrary.CQRS.Queries;
using BookLibrary.DTOs;
using BookLibrary.Interfaces;
using MediatR;

namespace BookLibrary.CQRS.Handlers;

public class SearchBooksQueryHandler : IRequestHandler<SearchBooksQuery, IReadOnlyList<BookDto>>
{
    private readonly IBookService _bookService;
    public SearchBooksQueryHandler(IBookService bookService) => _bookService = bookService;

    public async Task<IReadOnlyList<BookDto>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await _bookService.SearchAsync(request.SearchTerm, request.Status, request.SortBy);
        return books.Select(b => new BookDto(b.Id, b.Title, b.Author, b.Genre, b.Year, b.Status.ToString(), b.CreatedAt))
                    .ToList().AsReadOnly();
    }
}
