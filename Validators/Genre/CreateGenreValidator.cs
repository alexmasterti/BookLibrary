using BookLibrary.DTOs.Genre;
using FluentValidation;

namespace BookLibrary.Validators.Genre;

public class CreateGenreValidator : AbstractValidator<CreateGenreRequest>
{
    public CreateGenreValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}