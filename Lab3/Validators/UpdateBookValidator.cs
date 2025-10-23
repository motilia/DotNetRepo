using FluentValidation;
using Lab3.Features.Books;

namespace Lab3.Validators;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Author).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Year).InclusiveBetween(1450, DateTime.UtcNow.Year);
    }
}