using BookLibrary.DTOs;
using BookLibrary.Models;
using MediatR;
namespace BookLibrary.CQRS.Queries;
public record SearchBooksQuery(string SearchTerm, ReadingStatus? Status, string? SortBy) : IRequest<IReadOnlyList<BookDto>>;
