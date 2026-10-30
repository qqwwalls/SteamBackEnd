using FluentValidation;
using Steam.Application.DTOs.SystemRequirementsDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Validators;

public class SystemRequirementsCreateValidator : AbstractValidator<SystemRequirementsCreateDTO>
{
    public SystemRequirementsCreateValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Os).NotEmpty().WithMessage("Os обязательный");
    }
}
