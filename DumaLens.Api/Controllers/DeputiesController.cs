using AutoMapper;
using DumaLens.Api.Models.Deputies;
using DumaLens.Api.Models.Exceptions;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Карточки депутатов и списки депутатов созывов
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class DeputiesController : ControllerBase
{
    private readonly IDeputyCardService deputyCardService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DeputiesController(IDeputyCardService deputyCardService, IMapper mapper)
    {
        this.deputyCardService = deputyCardService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Список депутатов созыва (для фильтров интерфейса)
    /// </summary>
    /// <param name="convocation">Номер созыва</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DeputyListItemApiModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetByConvocation([FromQuery] int? convocation, CancellationToken cancellationToken)
    {
        var query = new DeputyListQueryModel
        {
            Convocation = convocation ?? 0,
        };
        var deputies = await deputyCardService.GetByConvocation(query, cancellationToken);
        return Ok(mapper.Map<IReadOnlyCollection<DeputyListItemApiModel>>(deputies));
    }

    /// <summary>
    /// Карточка депутата: фракции по созывам, доля участия, счётчики личных результатов
    /// </summary>
    /// <param name="id">Идентификатор депутата на сайте Госдумы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(DeputyApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCard(long id, CancellationToken cancellationToken)
    {
        var deputy = await deputyCardService.GetCard(id, cancellationToken);
        return Ok(mapper.Map<DeputyApiModel>(deputy));
    }
}