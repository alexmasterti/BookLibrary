using BookLibrary.DTOs.Book;
using MediatR;

namespace BookLibrary.CQRS.Queries;

/// <summary>
/// CQRS Query — represents the INTENT to read data.
/// Queries never change state. They are safe to retry, cache, and run in parallel.
/// </summary>
public record GetAllBooksQuery : IRequest<IReadOnlyList<BookDto>>;
