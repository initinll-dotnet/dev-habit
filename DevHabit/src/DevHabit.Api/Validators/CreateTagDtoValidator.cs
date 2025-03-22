using DevHabit.Api.DTOs.Tags;

using FluentValidation;

namespace DevHabit.Api.Validators;

public sealed class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(10);
        RuleFor(x => x.Description).MaximumLength(50);
    }
}
