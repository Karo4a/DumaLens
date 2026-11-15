using AutoMapper;
using DumaLens.Api.Models.Bills;
using DumaLens.Api.Models.Exceptions;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Карточки законопроектов и поиск по части номера
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class BillsController : ControllerBase
{
    private readonly IBillCardService billCardService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public BillsController(IBillCardService billCardService, IMapper mapper)
    {
        this.billCardService = billCardService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Поиск законопроектов по части номера (автодополнение)
    /// </summary>
    /// <param name="numberPart">Часть номера законопроекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<BillSearchItemApiModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Search([FromQuery] string? numberPart, CancellationToken cancellationToken)
    {
        var query = new BillSearchQueryModel
        {
            NumberPart = numberPart ?? string.Empty,
        };
        var bills = await billCardService.Search(query, cancellationToken);
        return Ok(mapper.Map<IReadOnlyCollection<BillSearchItemApiModel>>(bills));
    }

    /// <summary>
    /// Карточка законопроекта: номер, ссылка на СОЗД, хронология голосований с позициями фракций
    /// </summary>
    /// <param name="number">Номер законопроекта (например, 500091-8)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet("{number}")]
    [ProducesResponseType(typeof(BillApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCard(string number, CancellationToken cancellationToken)
    {
        var bill = await billCardService.GetCard(number, cancellationToken);
        return Ok(mapper.Map<BillApiModel>(bill));
    }
}