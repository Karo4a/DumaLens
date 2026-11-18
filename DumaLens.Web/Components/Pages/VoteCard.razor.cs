using DumaLens.Api.Client;
using Microsoft.AspNetCore.Components;

namespace DumaLens.Web.Components.Pages;

/// <summary>
/// Карточка голосования: матрица фракций и депутаты по фракциям
/// </summary>
public partial class VoteCard : ComponentBase
{
    /// <summary>
    /// Идентификатор голосования
    /// </summary>
    [Parameter]
    public long Id { get; set; }

    [Inject]
    private IVotesClient Votes { get; set; } = default!;

    private VoteDetailsApiModel? vote;
    private bool loading = true;
    private string? error;

    private int TotalDeputies => vote?.DeputyVoteGroups.Sum(g => g.Deputies.Count) ?? 0;

    /// <summary>
    /// Загрузка карточки при смене параметра маршрута
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        loading = true;
        error = null;
        try
        {
            vote = await Votes.GetDetailsAsync(Id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            vote = null;
            error = "Голосование не найдено.";
        }
        catch (ApiException ex)
        {
            error = "Не удалось загрузить голосование (код " + ex.StatusCode + ").";
        }
        catch (HttpRequestException)
        {
            error = "Сервис поиска недоступен.";
        }
        finally
        {
            loading = false;
        }
    }
}