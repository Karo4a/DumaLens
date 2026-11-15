using AutoMapper;
using DumaLens.Api.Models.Convocations;
using DumaLens.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Справочник созывов
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConvocationsController : ControllerBase
{
    private readonly IConvocationCatalogService convocationCatalogService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ConvocationsController(IConvocationCatalogService convocationCatalogService, IMapper mapper)
    {
        this.convocationCatalogService = convocationCatalogService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Справочник созывов: номер, код сайта, даты границ, число голосований
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConvocationApiModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var convocations = await convocationCatalogService.GetAll(cancellationToken);
        return Ok(mapper.Map<IReadOnlyCollection<ConvocationApiModel>>(convocations));
    }
}