using DumaLens.Services.Contracts.Exceptions;

namespace DumaLens.Api.Models.Exceptions;

/// <summary>
/// Информация об ошибке валидации работы API
/// </summary>
public class ApiValidationExceptionDetail
{
    /// <summary>
    /// Ошибки валидации
    /// </summary>
    public IEnumerable<InvalidateItemModel> Errors { get; set; } = Array.Empty<InvalidateItemModel>();
}