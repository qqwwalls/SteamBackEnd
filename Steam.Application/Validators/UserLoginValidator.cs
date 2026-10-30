using FluentValidation;
using Steam.Application.DTOs.UserDTOs;
using System.Text;

namespace Steam.Application.Validators;

public class UserLoginValidator : AbstractValidator<UserLoginDto>
{
    public UserLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty()
            .Must(password => password == null || Encoding.UTF8.GetByteCount(password) <= 72);
    }
}
