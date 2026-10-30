using FluentValidation;
using Steam.Application.DTOs.MediaDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Validators;

public class MediaCreateValidator : AbstractValidator<MediaCreateDTO>
{
    public MediaCreateValidator()
    {
        RuleFor(x => x.GameId).Must(id => !id.HasValue || id != Guid.Empty);
        RuleFor(x => x.Url).NotEmpty().WithMessage("Url обязательный");
        RuleFor(x => x.Url).Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Size).GreaterThanOrEqualTo(0);
    }
}
