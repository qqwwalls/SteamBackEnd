using FluentValidation;
using Steam.Application.DTOs.UserDTOs;

namespace Steam.Application.Validators;

public class UpdateAvatarValidator : AbstractValidator<UpdateAvatarDTO>
{
    public UpdateAvatarValidator()
    {
        RuleFor(x => x.AvatarUrl).NotEmpty().Must(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
    }
}
