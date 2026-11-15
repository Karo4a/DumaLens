using AutoMapper;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Cards;

/// <summary>
/// Сервис карточек голосований
/// </summary>
public class VoteCardService : IVoteCardService, IServiceAnchor
{
    private readonly IVoteReadRepository voteReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteCardService(IVoteReadRepository voteReadRepository, IMapper mapper)
    {
        this.voteReadRepository = voteReadRepository;
        this.mapper = mapper;
    }

    async Task<VoteDetailsModel> IVoteCardService.GetDetails(long siteId, CancellationToken cancellationToken)
    {
        var vote = await voteReadRepository.GetDetailsBySiteId(siteId, cancellationToken);
        if (vote == null)
        {
            throw new DumaLensNotFoundException($"Голосование с идентификатором {siteId} не найдено");
        }
        var model = mapper.Map<VoteDetailsModel>(vote);
        model.DeputyVoteGroups = GroupDeputyVotes(vote.DeputyVotes);
        return model;
    }

    private IReadOnlyCollection<DeputyVoteGroupModel> GroupDeputyVotes(IReadOnlyCollection<DeputyVoteDbModel> deputyVotes)
        => deputyVotes
            .GroupBy(x => x.FactionCode)
            .Select(group => new DeputyVoteGroupModel
            {
                FactionCode = group.Key,
                // В архиве есть созывы с двумя периодами одной фракции (Е-ЕР/О-ЕР, III созыв):
                // заголовок группы — название, под которым голосовало большинство; при равенстве — ordinal
                FactionTitle = group
                    .GroupBy(x => x.FactionTitle)
                    .OrderByDescending(x => x.Count())
                    .ThenBy(x => x.Key, StringComparer.Ordinal)
                    .First().Key,
                Deputies = mapper.Map<IReadOnlyCollection<DeputyVoteItemModel>>(group.ToList()),
            })
            .OrderBy(x => x.FactionCode == null ? 1 : 0)
            .ThenBy(x => x.FactionCode)
            .ToList();
}