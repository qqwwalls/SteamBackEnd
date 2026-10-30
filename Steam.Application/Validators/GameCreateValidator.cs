using FluentValidation;
using Steam.Application.DTOs.GameDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Validators;

public class GameCreateValidator : AbstractValidator<GameCreateDTO>
{
    public GameCreateValidator()
    {
        RuleFor(x => x.DeveloperId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.GenreIds).NotNull();
        RuleFor(x => x.TagIds).NotNull();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название обязательно")
            .MaximumLength(200).WithMessage("Название не может превышать 200 символов");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Цена должна быть неотрицательной");
    }
}
