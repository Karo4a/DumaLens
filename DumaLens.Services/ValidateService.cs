using DumaLens.Services.Auth;
using DumaLens.Services.Cards;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Search;
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
        validators = new Dictionary<Type, IValidator>
        {
            [typeof(VoteSearchQueryModel)] = new VoteSearchQueryModelValidator(),
            [typeof(BillSearchQueryModel)] = new BillSearchQueryModelValidator(),
            [typeof(DeputyListQueryModel)] = new DeputyListQueryModelValidator(),
            [typeof(LoginModel)] = new LoginModelValidator(),
            [typeof(RefreshRequestModel)] = new RefreshRequestModelValidator(),
            [typeof(ChangePasswordModel)] = new ChangePasswordModelValidator(),
            [typeof(CreateUserModel)] = new CreateUserModelValidator(),
            [typeof(UpdateUserModel)] = new UpdateUserModelValidator(),
            [typeof(UserListQueryModel)] = new UserListQueryModelValidator(),
        };
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