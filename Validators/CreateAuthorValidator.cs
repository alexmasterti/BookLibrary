using BookLibrary.DTOs;
using FluentValidation;

namespace BookLibrary.Validators;

/// <summary>
/// Validation rules for creating an author.
///
/// CONCEPT: FluentValidation
///   Validation lives here — not in controllers, not in models.
///   Each rule is readable, composable, and independently testable.
/// </summary>
public class CreateAuthorValidator : AbstractValidator<CreateAuthorRequest>
{
    public CreateAuthorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.")
            .When(x => x.Bio is not null);

        RuleFor(x => x.BirthYear)
            .InclusiveBetween(1000, DateTime.UtcNow.Year)
            .WithMessage($"Birth year must be between 1000 and {DateTime.UtcNow.Year}.")
            .When(x => x.BirthYear.HasValue);

        RuleFor(x => x.Nationality)
            .MaximumLength(100).WithMessage("Nationality must not exceed 100 characters.")
            .When(x => x.Nationality is not null);
    }
}
