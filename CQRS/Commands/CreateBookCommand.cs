using BookLibrary.DTOs;
using MediatR;

namespace BookLibrary.CQRS.Commands;

/// <summary>
/// CQRS Command — represents the INTENT to create a book.
/// Commands change state. They are named as verbs in imperative form.
/// Using a record makes them immutable — a command cannot be modified after creation.
/// </summary>
public record CreateBookCommand(
    string Title,
    string Author,
    string? Genre,
    int? Year,
    string? Status) : IRequest<BookDto>;
