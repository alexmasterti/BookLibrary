using BookLibrary.DTOs.Book;
using MediatR;
namespace BookLibrary.CQRS.Queries;
public record GetBookByIdQuery(int Id) : IRequest<BookDto?>;
