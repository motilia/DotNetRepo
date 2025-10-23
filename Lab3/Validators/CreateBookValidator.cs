using FluentValidation;
using Lab3.Features.Books;

namespace Lab3.Validators;

public class CreateBookValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Author).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Year).InclusiveBetween(1450, DateTime.UtcNow.Year);
    }
}