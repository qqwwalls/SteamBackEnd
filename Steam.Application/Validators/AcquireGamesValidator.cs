using FluentValidation;
using Steam.Application.DTOs.LibraryDTOs;

namespace Steam.Application.Validators;

public class AcquireGamesValidator : AbstractValidator<AcquireGamesDTO>
{
    public AcquireGamesValidator()
    {
        RuleFor(x => x.GameIds).NotEmpty().Must(ids => ids == null || ids.Count <= 50);
        RuleForEach(x => x.GameIds).NotEmpty();
    }
}
