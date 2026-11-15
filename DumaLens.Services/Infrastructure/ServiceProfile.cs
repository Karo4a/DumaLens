using AutoMapper;
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
    }
}