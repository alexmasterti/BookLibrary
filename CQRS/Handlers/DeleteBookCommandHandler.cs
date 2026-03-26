using BookLibrary.CQRS.Commands;
using BookLibrary.Interfaces.Books;
using BookLibrary.Interfaces.Authors;
using BookLibrary.Interfaces.Common;
using MediatR;

namespace BookLibrary.CQRS.Handlers;

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, bool>
{
    private readonly IBookService _bookService;
    public DeleteBookCommandHandler(IBookService bookService) => _bookService = bookService;

    public async Task<bool> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        await _bookService.DeleteBookAsync(request.Id);
        return true;
    }
}
