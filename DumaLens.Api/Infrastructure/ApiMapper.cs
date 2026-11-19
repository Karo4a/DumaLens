using AutoMapper;
using DumaLens.Api.Models.Auth;
using DumaLens.Api.Models.Bills;
using DumaLens.Api.Models.Convocations;
using DumaLens.Api.Models.Deputies;
using DumaLens.Api.Models.Factions;
using DumaLens.Api.Models.Stats;
using DumaLens.Api.Models.Users;
using DumaLens.Api.Models.Votes;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Api.Infrastructure;

/// <summary>
/// Профиль автомаппинга API
/// </summary>
public class ApiMapper : Profile
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public ApiMapper()
    {
        CreateMap<SearchVotesRequestApiModel, VoteSearchQueryModel>()
            .ForMember(destination => destination.Query, option => option.MapFrom(source => source.Q))
            .ForMember(destination => destination.HasResults, option => option.Ignore());
        CreateMap<FactionConditionRequestApiModel, FactionConditionQueryModel>();
        CreateMap<VoteSearchResultModel, VoteSearchResultApiModel>();
        CreateMap<VoteSearchItemModel, VoteApiModel>()
            .ForMember(destination => destination.Id, option => option.MapFrom(source => source.SiteId));
        CreateMap<VoteFactionStatsItemModel, VoteFactionStatsApiModel>();
        CreateMap<VoteDetailsModel, VoteDetailsApiModel>()
            .IncludeBase<VoteSearchItemModel, VoteApiModel>();
        CreateMap<DeputyVoteGroupModel, DeputyVoteGroupApiModel>();
        CreateMap<DeputyVoteItemModel, DeputyVoteApiModel>()
            .ForMember(destination => destination.Id, option => option.MapFrom(source => source.DeputySiteId));
        CreateMap<BillCardModel, BillApiModel>();
        CreateMap<BillVoteModel, BillVoteApiModel>()
            .ForMember(destination => destination.Id, option => option.MapFrom(source => source.SiteId));
        CreateMap<BillFactionPositionModel, BillFactionPositionApiModel>();
        CreateMap<BillSearchItemModel, BillSearchItemApiModel>();
        CreateMap<FactionCardModel, FactionApiModel>();
        CreateMap<FactionPeriodModel, FactionPeriodApiModel>()
            .ForMember(destination => destination.Convocation, option => option.MapFrom(source => source.ConvocationNumber));
        CreateMap<FactionConvocationStatsModel, FactionConvocationStatsApiModel>()
            .ForMember(destination => destination.Convocation, option => option.MapFrom(source => source.ConvocationNumber));
        CreateMap<DeputyCardModel, DeputyApiModel>()
            .ForMember(destination => destination.Id, option => option.MapFrom(source => source.SiteId));
        CreateMap<DeputyMembershipModel, DeputyMembershipApiModel>()
            .ForMember(destination => destination.Convocation, option => option.MapFrom(source => source.ConvocationNumber));
        CreateMap<DeputyListItemModel, DeputyListItemApiModel>()
            .ForMember(destination => destination.Id, option => option.MapFrom(source => source.SiteId));
        CreateMap<ConvocationCatalogItemModel, ConvocationApiModel>();
        CreateMap<StatsOverviewModel, StatsOverviewApiModel>();
        // Auth (Э9)
        CreateMap<LoginRequestApiModel, LoginModel>();
        CreateMap<RefreshRequestApiModel, RefreshRequestModel>();
        CreateMap<ChangePasswordRequestApiModel, ChangePasswordModel>();
        CreateMap<AuthResultModel, AuthResponseApiModel>();
        CreateMap<UserProfileModel, UserProfileApiModel>();
        CreateMap<CreateUserRequestApiModel, CreateUserModel>();
        CreateMap<UpdateUserRequestApiModel, UpdateUserModel>();
        CreateMap<UserListItemModel, UserApiModel>();
        CreateMap<UserPageModel, UserPageApiModel>();
        CreateMap<ResetPasswordResultModel, ResetPasswordResponseApiModel>();
    }
}