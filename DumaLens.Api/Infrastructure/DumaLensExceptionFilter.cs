using DumaLens.Api.Models.Exceptions;
using DumaLens.Services.Contracts.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DumaLens.Api.Infrastructure;

/// <summary>
/// Фильтр обработки доменных исключений
/// </summary>
public class DumaLensExceptionFilter : IExceptionFilter
{
    private readonly ILogger<DumaLensExceptionFilter> logger;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensExceptionFilter(ILogger<DumaLensExceptionFilter> logger)
    {
        this.logger = logger;
    }

    void IExceptionFilter.OnException(ExceptionContext context)
    {
        if (context.Exception is not DumaLensException exception)
        {
            return;
        }
        switch (exception)
        {
            case DumaLensNotFoundException ex:
                SetDataToContext(new NotFoundObjectResult(new ApiExceptionDetail(ex.Message)), context);
                break;
            case DumaLensUnauthorizedException ex:
                SetDataToContext(new ObjectResult(new ApiExceptionDetail(ex.Message))
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                }, context);
                break;
            case DumaLensValidationException ex:
                SetDataToContext(new ObjectResult(new ApiValidationExceptionDetail
                {
                    Errors = ex.Errors,
                })
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity,
                }, context);
                break;
            case DumaLensDuplicateException ex:
                SetDataToContext(new ConflictObjectResult(new ApiExceptionDetail(ex.Message)), context);
                break;
            default:
                logger.LogError(exception, "Необработанное доменное исключение");
                SetDataToContext(new ObjectResult(new ApiExceptionDetail(exception.Message))
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                }, context);
                break;
        }
    }

    private static void SetDataToContext(ObjectResult data, ExceptionContext context)
    {
        context.ExceptionHandled = true;
        var response = context.HttpContext.Response;
        response.StatusCode = data.StatusCode ?? StatusCodes.Status500InternalServerError;
        context.Result = data;
    }
}