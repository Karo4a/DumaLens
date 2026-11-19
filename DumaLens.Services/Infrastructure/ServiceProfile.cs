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
        // Auth (Э9): проекции чтения и обратный маппинг полной проекции в сущность для записи (паттерн DG master)
        CreateMap<UserDbModel, UserProfileModel>()
            .ForMember(destination => destination.Role, option => option.MapFrom(source => source.Role.ToString()));
        CreateMap<UserDbModel, User>()
            .ForMember(destination => destination.LoginLowerCase, option => option.MapFrom(source => source.Login.ToLowerInvariant()))
            .ForMember(destination => destination.EmailLowerCase, option => option.MapFrom(source => source.Email.ToLowerInvariant()))
            .ForMember(destination => destination.Role, option => option.Ignore())
            .ForMember(destination => destination.UpdatedAt, option => option.Ignore())
            .ForMember(destination => destination.DeletedAt, option => option.Ignore());
        CreateMap<User, UserListItemModel>()
            .ForMember(destination => destination.Role, option => option.MapFrom(source => source.Role.Role.ToString()));
        CreateMap<UserDbModel, UserListItemModel>()
            .ForMember(destination => destination.Role, option => option.MapFrom(source => source.Role.ToString()));
        CreateMap<UserListItemDbModel, UserListItemModel>()
            .ForMember(destination => destination.Role, option => option.MapFrom(source => source.Role.ToString()));
        CreateMap<UserPageDbModel, UserPageModel>();
        CreateMap<RefreshTokenDbModel, RefreshToken>();
    }
}