using AutoMapper;
using DumaLens.Api.Models.Stats;
using DumaLens.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Сводные показатели базы
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly IStatsOverviewService statsOverviewService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public StatsController(IStatsOverviewService statsOverviewService, IMapper mapper)
    {
        this.statsOverviewService = statsOverviewService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Сводка для главной страницы: счётчики сущностей и диапазон дат голосований
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(StatsOverviewApiModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await statsOverviewService.GetOverview(cancellationToken);
        return Ok(mapper.Map<StatsOverviewApiModel>(overview));
    }
}