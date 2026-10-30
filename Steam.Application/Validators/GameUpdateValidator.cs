using FluentValidation;
using Steam.Application.DTOs.GameDTOs;

namespace Steam.Application.Validators;

public class GameUpdateValidator : AbstractValidator<GameUpdateDTO>
{
    public GameUpdateValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
