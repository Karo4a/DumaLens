using AutoMapper;
using DumaLens.Api.Models.Exceptions;
using DumaLens.Api.Models.Votes;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Поиск голосований и карточки голосований
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class VotesController : ControllerBase
{
    private readonly IVoteSearchService voteSearchService;
    private readonly IVoteCardService voteCardService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VotesController(IVoteSearchService voteSearchService, IVoteCardService voteCardService, IMapper mapper)
    {
        this.voteSearchService = voteSearchService;
        this.voteCardService = voteCardService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Поиск голосований: полнотекстовый запрос, фильтры, фракционные условия, пагинация
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(VoteSearchResultApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Search([FromQuery] SearchVotesRequestApiModel request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<VoteSearchQueryModel>(request);
        var result = await voteSearchService.Search(query, cancellationToken);
        return Ok(mapper.Map<VoteSearchResultApiModel>(result));
    }

    /// <summary>
    /// Полная карточка голосования: сводные счётчики, статистика фракций, голоса депутатов по фракциям
    /// </summary>
    /// <param name="id">Идентификатор голосования на сайте Госдумы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(VoteDetailsApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(long id, CancellationToken cancellationToken)
    {
        var vote = await voteCardService.GetDetails(id, cancellationToken);
        return Ok(mapper.Map<VoteDetailsApiModel>(vote));
    }
}