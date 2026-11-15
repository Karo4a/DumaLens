using DumaLens.Entities.ValidationConstants;
using DumaLens.Services.Contracts.Models;
using FluentValidation;

namespace DumaLens.Services.Cards;

/// <summary>
/// Валидатор запроса списка депутатов созыва
/// </summary>
public class DeputyListQueryModelValidator : AbstractValidator<DeputyListQueryModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DeputyListQueryModelValidator()
    {
        RuleFor(x => x.Convocation)
            .InclusiveBetween(VoteSearchValidationConstants.ConvocationNumberMin, VoteSearchValidationConstants.ConvocationNumberMax)
            .WithMessage($"Номер созыва должен быть в диапазоне от {VoteSearchValidationConstants.ConvocationNumberMin} до {VoteSearchValidationConstants.ConvocationNumberMax}");
    }
}