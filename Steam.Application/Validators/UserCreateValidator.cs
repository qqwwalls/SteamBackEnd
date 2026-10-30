using FluentValidation;
using Steam.Application.DTOs.UserDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Validators;

public class UserCreateValidator : AbstractValidator<UserCreateDTO>
{
    public UserCreateValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MaximumLength(100).WithMessage("Имя не может превышать 100 символов");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Неверный формат email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен быть не менее 6 символов");
        RuleFor(x => x.Password).Must(password => password == null || Encoding.UTF8.GetByteCount(password) <= 72)
            .WithMessage("Password must not exceed 72 UTF-8 bytes.");
        RuleFor(x => x.AvatarUrl).Must(url => string.IsNullOrWhiteSpace(url) ||
            (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)));
    }
}
