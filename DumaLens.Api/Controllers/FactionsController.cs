using AutoMapper;
using DumaLens.Api.Models.Exceptions;
using DumaLens.Api.Models.Factions;
using DumaLens.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Канонические фракции и карточки фракций
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class FactionsController : ControllerBase
{
    private readonly IFactionCardService factionCardService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionsController(IFactionCardService factionCardService, IMapper mapper)
    {
        this.factionCardService = factionCardService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Канонические фракции с историческими периодами
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<FactionApiModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var factions = await factionCardService.GetAll(cancellationToken);
        return Ok(mapper.Map<IReadOnlyCollection<FactionApiModel>>(factions));
    }

    /// <summary>
    /// Карточка фракции: периоды и сводная статистика по созывам
    /// </summary>
    /// <param name="code">Код канонической фракции (например, ER)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(FactionApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCard(string code, CancellationToken cancellationToken)
    {
        var faction = await factionCardService.GetCard(code, cancellationToken);
        return Ok(mapper.Map<FactionApiModel>(faction));
    }
}