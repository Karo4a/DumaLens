using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Repositories.Contracts.Enums;
using DumaLens.Services.Contracts.Models;
using FluentValidation;

namespace DumaLens.Services.Search;

/// <summary>
/// Валидатор запроса поиска голосований (ФТ-20–ФТ-24, контракт 04-functionality.md §3)
/// </summary>
public class VoteSearchQueryModelValidator : AbstractValidator<VoteSearchQueryModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteSearchQueryModelValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(VoteSearchValidationConstants.QueryMaxLength)
            .When(x => x.Query != null)
            .WithMessage($"Длина текстового запроса не должна превышать {VoteSearchValidationConstants.QueryMaxLength} символов");

        RuleFor(x => x.Convocation)
            .InclusiveBetween(VoteSearchValidationConstants.ConvocationNumberMin, VoteSearchValidationConstants.ConvocationNumberMax)
            .When(x => x.Convocation.HasValue)
            .WithMessage($"Номер созыва должен быть в диапазоне от {VoteSearchValidationConstants.ConvocationNumberMin} до {VoteSearchValidationConstants.ConvocationNumberMax}");

        RuleFor(x => x.BillNumber)
            .MaximumLength(BillValidationConstants.NumberMaxLength)
            .When(x => x.BillNumber != null)
            .WithMessage($"Длина номера законопроекта не должна превышать {BillValidationConstants.NumberMaxLength} символов");

        RuleFor(x => x.Stage)
            .Must(stage => SearchTokenParser.ParseStage(stage) != null)
            .When(x => x.Stage != null)
            .WithMessage((x, stage) => $"Неизвестная стадия голосования: «{stage}». Допустимы: FirstReading, SecondReading, SecondReadingAcceptedAmendments, SecondReadingRejectedAmendments, ThirdReading, AsBasis, AsWhole, Reject, ReturnToSecondReading, Procedural, Unknown");

        RuleFor(x => x.Outcome)
            .Must(outcome => SearchTokenParser.ParseOutcome(outcome) != null)
            .When(x => x.Outcome != null)
            .WithMessage((x, outcome) => $"Неизвестный исход голосования: «{outcome}». Допустимы: Accepted, Rejected, Unknown");

        RuleFor(x => x.Sort)
            .Must(sort => SearchTokenParser.ParseSort(sort) != null)
            .When(x => x.Sort != null)
            .WithMessage((x, sort) => $"Неизвестная сортировка: «{sort}». Допустимы: dateDesc, dateAsc, relevance, acceptCountDesc");

        RuleFor(x => x.Sort)
            .Must((query, sort) => SearchTokenParser.ParseSort(sort) != VoteSearchSort.Relevance || !string.IsNullOrWhiteSpace(query.Query))
            .WithMessage("Сортировка по релевантности доступна только с текстовым запросом");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("dateFrom не может быть позже dateTo");

        RuleFor(x => x.Page)
            .InclusiveBetween(VoteSearchValidationConstants.PageMin, VoteSearchValidationConstants.PageMax)
            .WithMessage($"Номер страницы должен быть в диапазоне от {VoteSearchValidationConstants.PageMin} до {VoteSearchValidationConstants.PageMax}");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(VoteSearchValidationConstants.PageSizeMin, VoteSearchValidationConstants.PageSizeMax)
            .WithMessage($"Размер страницы должен быть в диапазоне от {VoteSearchValidationConstants.PageSizeMin} до {VoteSearchValidationConstants.PageSizeMax}");

        RuleFor(x => x.FactionConditions)
            .NotNull()
            .WithMessage("Список фракционных условий не может быть null");

        RuleFor(x => x.FactionConditions)
            .Must(conditions => conditions.Count <= VoteSearchValidationConstants.FactionConditionsMax)
            .When(x => x.FactionConditions != null)
            .WithMessage($"Число фракционных условий не должно превышать {VoteSearchValidationConstants.FactionConditionsMax}");

        RuleForEach(x => x.FactionConditions)
            .SetValidator(new FactionConditionQueryModelValidator());
    }
}

/// <summary>
/// Валидатор фракционного условия поиска
/// </summary>
public class FactionConditionQueryModelValidator : AbstractValidator<FactionConditionQueryModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionConditionQueryModelValidator()
    {
        RuleFor(x => x.Faction)
            .NotEmpty()
            .WithMessage("Фракция обязательна")
            .MaximumLength(CommonValidationConstants.TitleMaxLength)
            .WithMessage($"Длина названия фракции не должна превышать {CommonValidationConstants.TitleMaxLength} символов");

        RuleFor(x => x.Result)
            .Must(result => SearchTokenParser.ParseResult(result) != null)
            .WithMessage((x, result) => $"Неизвестный результат фракции: «{result}». Допустимы: accept, decline, abstain, none");

        RuleFor(x => x.Operator)
            .Must(operatorToken => SearchTokenParser.ParseOperator(operatorToken) != null)
            .WithMessage((x, operatorToken) => $"Неизвестный оператор сравнения: «{operatorToken}». Допустимы: gte, lte");

        RuleFor(x => x.Threshold)
            .InclusiveBetween(CommonValidationConstants.PercentMin, CommonValidationConstants.PercentMax)
            .WithMessage($"Порог должен быть в диапазоне от {CommonValidationConstants.PercentMin} до {CommonValidationConstants.PercentMax}");

        RuleFor(x => x.Basis)
            .Must(basis => SearchTokenParser.ParseBasis(basis) != null)
            .WithMessage((x, basis) => $"Неизвестная база процента: «{basis}». Допустимы: members, voted");

        RuleFor(x => x.Basis)
            .Must((condition, basis) => !(SearchTokenParser.ParseResult(condition.Result) == VoteResult.None && SearchTokenParser.ParseBasis(basis) == PercentBasis.Voted))
            .WithMessage("База «проголосовавшие» (voted) недоступна для результата «не голосовал» (none)");
    }
}