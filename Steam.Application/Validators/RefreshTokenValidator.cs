using FluentValidation;
using Steam.Application.DTOs.AuthDTOs;

namespace Steam.Application.Validators;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequestDTO>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}
