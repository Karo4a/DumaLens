namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис валидации
/// </summary>
public interface IValidateService
{
    /// <summary>
    /// Выполняет валидацию <typeparamref name="TModel"/>
    /// </summary>
    Task Validate<TModel>(TModel model, CancellationToken cancellation)
        where TModel : class;
}