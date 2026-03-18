using MediatR;
namespace BookLibrary.CQRS.Commands;
public record DeleteBookCommand(int Id) : IRequest<bool>;
