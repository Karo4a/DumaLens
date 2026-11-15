using DumaLens.Entities.ValidationConstants;
using DumaLens.Services.Contracts.Models;
using FluentValidation;

namespace DumaLens.Services.Cards;

/// <summary>
/// Валидатор запроса поиска законопроектов по части номера
/// </summary>
public class BillSearchQueryModelValidator : AbstractValidator<BillSearchQueryModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public BillSearchQueryModelValidator()
    {
        RuleFor(x => x.NumberPart)
            .NotEmpty()
            .WithMessage("Часть номера законопроекта обязательна");

        RuleFor(x => x.NumberPart)
            .MaximumLength(BillValidationConstants.NumberMaxLength)
            .WithMessage($"Длина части номера законопроекта не должна превышать {BillValidationConstants.NumberMaxLength} символов");
    }
}