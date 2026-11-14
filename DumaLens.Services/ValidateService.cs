using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using FluentValidation;

namespace DumaLens.Services;

/// <inheritdoc cref="IValidateService"/>
public class ValidateService : IValidateService, IServiceAnchor
{
    private readonly IDictionary<Type, IValidator> validators;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ValidateService()
    {
        // Валидаторы моделей появятся на Э5+
        validators = new Dictionary<Type, IValidator>();
    }

    async Task IValidateService.Validate<TModel>(TModel model, CancellationToken cancellation)
        where TModel : class
    {
        if (!validators.TryGetValue(model.GetType(), out var validator))
        {
            throw new InvalidOperationException($"Не найден запрашиваемый валидатор {model.GetType()}");
        }
        var context = new ValidationContext<TModel>(model);
        var validationResult = await validator.ValidateAsync(context, cancellation);
        if (!validationResult.IsValid)
        {
            throw new DumaLensValidationException(validationResult.Errors.Select(x =>
                InvalidateItemModel.New(x.PropertyName, x.ErrorMessage)));
        }
    }
}