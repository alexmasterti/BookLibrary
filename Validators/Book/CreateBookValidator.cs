using BookLibrary.DTOs.Book;
using FluentValidation;

namespace BookLibrary.Validators.Book;

/// <summary>
/// CONCEPT: FluentValidation
///   Validation rules live in dedicated classes, not in controllers or models.
///   Each rule is explicit, testable, and produces clear error messages.
///   Replaces manual if-checks in controller actions.
/// </summary>
public class CreateBookValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters.");

        RuleFor(x => x.Genre)
            .MaximumLength(100).WithMessage("Genre must not exceed 100 characters.")
            .When(x => x.Genre is not null);

        RuleFor(x => x.Year)
            .InclusiveBetween(1000, DateTime.UtcNow.Year)
            .WithMessage($"Year must be between 1000 and {DateTime.UtcNow.Year}.")
            .When(x => x.Year.HasValue);

        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.TryParse<Models.ReadingStatus>(s, true, out _))
            .WithMessage("Status must be one of: WantToRead, CurrentlyReading, Read.")
            .When(x => x.Status is not null);
    }
}
