using AutoMapper;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Infrastructure;

/// <summary>
/// Профиль автомаппинга сервиса
/// </summary>
public class ServiceProfile : Profile
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public ServiceProfile()
    {
        CreateMap<VoteSearchPageDbModel, VoteSearchResultModel>();
        CreateMap<VoteSearchDbModel, VoteSearchItemModel>()
            .ForMember(destination => destination.Convocation, option => option.MapFrom(source => source.ConvocationNumber))
            .ForMember(destination => destination.VotedAt, option => option.MapFrom(source => source.VotedAt.ToOffset(TimeSpan.FromMinutes(source.UtcOffsetMinutes))));
        CreateMap<VoteFactionStatsDbModel, VoteFactionStatsItemModel>();
        CreateMap<VoteDetailsDbModel, VoteDetailsModel>()
            .IncludeBase<VoteSearchDbModel, VoteSearchItemModel>()
            .ForMember(destination => destination.DeputyVoteGroups, option => option.Ignore());
        CreateMap<DeputyVoteDbModel, DeputyVoteItemModel>();
        CreateMap<BillDetailsDbModel, BillCardModel>()
            .ForMember(destination => destination.SozdUrl, option => option.MapFrom(source => Bill.SozdUrlPrefix + source.Number));
        CreateMap<BillVoteDbModel, BillVoteModel>()
            .ForMember(destination => destination.VotedAt, option => option.MapFrom(source => source.VotedAt.ToOffset(TimeSpan.FromMinutes(source.UtcOffsetMinutes))));
        CreateMap<BillFactionPositionDbModel, BillFactionPositionModel>();
        CreateMap<BillDbModel, BillSearchItemModel>()
            .ForMember(destination => destination.SozdUrl, option => option.MapFrom(source => Bill.SozdUrlPrefix + source.Number));
        CreateMap<FactionDbModel, FactionCardModel>()
            .ForMember(destination => destination.Periods, option => option.Ignore())
            .ForMember(destination => destination.ConvocationStats, option => option.Ignore());
        CreateMap<FactionPeriodDbModel, FactionPeriodModel>();
        CreateMap<FactionConvocationStatsDbModel, FactionConvocationStatsModel>();
        CreateMap<DeputyCardDbModel, DeputyCardModel>()
            .ForMember(destination => destination.Factions, option => option.MapFrom(source => source.Memberships))
            .ForMember(destination => destination.TotalVotes, option => option.Ignore())
            .ForMember(destination => destination.ParticipationPct, option => option.Ignore());
        CreateMap<DeputyMembershipDbModel, DeputyMembershipModel>();
        CreateMap<DeputyListItemDbModel, DeputyListItemModel>();
        CreateMap<ConvocationCatalogDbModel, ConvocationCatalogItemModel>();
        CreateMap<StatsOverviewDbModel, StatsOverviewModel>()
            .ForMember(destination => destination.TotalVotes, option => option.MapFrom(source => source.VoteCount))
            .ForMember(destination => destination.TotalDeputies, option => option.MapFrom(source => source.DeputyCount))
            .ForMember(destination => destination.TotalFactions, option => option.MapFrom(source => source.FactionCount))
            .ForMember(destination => destination.TotalBills, option => option.MapFrom(source => source.BillCount))
            .ForMember(destination => destination.DateFrom, option => option.MapFrom(source => source.DateFrom.HasValue ? DateOnly.FromDateTime(source.DateFrom.Value.DateTime) : (DateOnly?)null))
            .ForMember(destination => destination.DateTo, option => option.MapFrom(source => source.DateTo.HasValue ? DateOnly.FromDateTime(source.DateTo.Value.DateTime) : (DateOnly?)null));
    }
}