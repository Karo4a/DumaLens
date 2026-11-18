using DumaLens.Api.Client;
using DumaLens.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DumaLens.Web.Components.Pages;

/// <summary>
/// Поиск голосований: фильтры и конструктор фракционных условий, состояние в URL
/// </summary>
public partial class Search : ComponentBase, IDisposable
{
    private const int DefaultPageSize = 20;

    [Inject]
    private IVotesClient Votes { get; set; } = default!;

    [Inject]
    private IFactionsClient Factions { get; set; } = default!;

    [Inject]
    private IConvocationsClient Convocations { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private IReadOnlyList<FactionApiModel> factions = [];
    private IReadOnlyList<ConvocationApiModel> convocations = [];
    private List<FactionConditionUi> conditions = [];

    private string q = string.Empty;
    private int? convocation;
    private string dateFrom = string.Empty;
    private string dateTo = string.Empty;
    private string billNumber = string.Empty;
    private string stage = string.Empty;
    private string outcome = string.Empty;
    private string sort = "dateDesc";
    private int page = 1;

    private VoteSearchResultApiModel? result;
    private bool loading = true;
    private string? error;
    private string? lastUrl;
    private int searchSequence;

    /// <summary>
    /// Загрузка справочников для фильтров и подписка на смену URL
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        Nav.LocationChanged += OnLocationChanged;
        try
        {
            factions = (await Factions.GetAllAsync()).OrderBy(f => UiFormat.FactionSortKey(f.Code)).ToList();
        }
        catch (ApiException)
        {
            factions = [];
        }
        catch (HttpRequestException)
        {
            factions = [];
        }

        try
        {
            convocations = (await Convocations.GetAllAsync()).OrderByDescending(c => c.Number).ToList();
        }
        catch (ApiException)
        {
            convocations = [];
        }
        catch (HttpRequestException)
        {
            convocations = [];
        }
    }

    /// <summary>
    /// Разбор состояния фильтров из URL и выполнение поиска
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        await ProcessCurrentUrl();
    }

    /// <summary>
    /// Смена URL (пагинация, «Найти», сортировка): повторный поиск и перерисовка.
    /// OnParametersSetAsync не срабатывает при смене только query-строки, поэтому нужна подписка.
    /// </summary>
    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // Обработчик срабатывает на любую навигацию, включая уход со страницы — чужие URL не обрабатываем
        if (Nav.ToAbsoluteUri(e.Location).AbsolutePath != "/search")
        {
            return;
        }

        await ProcessCurrentUrl();
        StateHasChanged();
    }

    /// <summary>
    /// Обработка текущего URL с защитой от повторного запуска для того же адреса
    /// </summary>
    private async Task ProcessCurrentUrl()
    {
        var url = Nav.Uri;
        if (url == lastUrl)
        {
            return;
        }

        lastUrl = url;
        ParseUrl();
        await RunSearch();
    }

    /// <summary>
    /// Отписка от смены URL
    /// </summary>
    public void Dispose()
    {
        Nav.LocationChanged -= OnLocationChanged;
    }

    /// <summary>
    /// Перерисовка заполнения слайдеров после рендера
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("dlPaintSliders");
    }

    private void ApplySearch()
    {
        page = 1;
        Nav.NavigateTo(BuildUrl());
    }

    private void ResetFilters()
    {
        q = string.Empty;
        convocation = null;
        dateFrom = string.Empty;
        dateTo = string.Empty;
        billNumber = string.Empty;
        stage = string.Empty;
        outcome = string.Empty;
        sort = "dateDesc";
        page = 1;
        conditions = [];
        Nav.NavigateTo("/search");
    }

    private async Task OnPageChanged(int newPage)
    {
        page = newPage;
        Nav.NavigateTo(BuildUrl());
        await Task.CompletedTask;
    }

    private async Task RunSearch()
    {
        var sequence = ++searchSequence;
        loading = true;
        error = null;
        try
        {
            var requestConditions = conditions
                .Select(c => new FactionConditionRequestApiModel
                {
                    Faction = c.Faction,
                    Result = c.Result,
                    Operator = c.Operator,
                    Threshold = c.Threshold,
                    Basis = c.Basis,
                })
                .ToList();

            var response = await Votes.SearchWithConditionsAsync(
                NullIfEmpty(q),
                convocation,
                ParseDate(dateFrom),
                ParseDate(dateTo),
                NullIfEmpty(billNumber),
                NullIfEmpty(stage),
                NullIfEmpty(outcome),
                sort,
                page,
                DefaultPageSize,
                requestConditions.Count > 0 ? requestConditions : null);

            // Ответ устаревшего запроса (быстрая смена страницы/сортировки) не перезаписывает свежий
            if (sequence == searchSequence)
            {
                result = response;
            }
        }
        catch (ApiException<ApiValidationExceptionDetail> ex)
        {
            if (sequence == searchSequence)
            {
                result = null;
                error = "Ошибка фильтров: " + string.Join("; ", ex.Result?.Errors?.Select(e => e.Message) ?? []);
            }
        }
        catch (ApiException ex)
        {
            if (sequence == searchSequence)
            {
                result = null;
                error = "Не удалось выполнить поиск (код " + ex.StatusCode + ").";
            }
        }
        catch (HttpRequestException)
        {
            if (sequence == searchSequence)
            {
                result = null;
                error = "Сервис поиска недоступен.";
            }
        }
        finally
        {
            if (sequence == searchSequence)
            {
                loading = false;
            }
        }
    }

    private void ParseUrl()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in query)
        {
            map[kv.Key] = kv.Value.ToString();
        }

        q = Get(map, "q");
        convocation = int.TryParse(Get(map, "convocation"), out var conv) ? conv : null;
        dateFrom = Get(map, "dateFrom");
        dateTo = Get(map, "dateTo");
        billNumber = Get(map, "billNumber");
        stage = Get(map, "stage");
        outcome = Get(map, "outcome");
        sort = string.IsNullOrEmpty(Get(map, "sort")) ? "dateDesc" : Get(map, "sort");
        page = int.TryParse(Get(map, "page"), out var p) && p > 0 ? p : 1;

        var parsed = new Dictionary<int, FactionConditionUi>();
        foreach (var kv in query)
        {
            var match = Regex.Match(kv.Key, @"^factionConditions\[(\d+)\]\.(\w+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            // Индекс из URL может переполнить int — крафтовая ссылка не должна ронять пререндер
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                continue;
            }

            // Лимит API — лишние условия не парсим (защита от гигантских URL)
            if (parsed.Count >= ConditionBuilder.MaxConditions && !parsed.ContainsKey(index))
            {
                continue;
            }

            if (!parsed.TryGetValue(index, out var condition))
            {
                condition = new FactionConditionUi();
                parsed[index] = condition;
            }

            var value = kv.Value.ToString();
            switch (match.Groups[2].Value.ToLowerInvariant())
            {
                case "faction":
                    condition.Faction = value;
                    break;
                case "result":
                    condition.Result = value;
                    break;
                case "operator":
                    condition.Operator = value;
                    break;
                case "threshold":
                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var threshold))
                    {
                        condition.Threshold = threshold;
                    }

                    break;
                case "basis":
                    condition.Basis = value;
                    break;
            }
        }

        conditions = parsed.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
    }

    private string BuildUrl()
    {
        var parameters = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            parameters["q"] = q.Trim();
        }

        if (convocation.HasValue)
        {
            parameters["convocation"] = convocation.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrEmpty(dateFrom))
        {
            parameters["dateFrom"] = dateFrom;
        }

        if (!string.IsNullOrEmpty(dateTo))
        {
            parameters["dateTo"] = dateTo;
        }

        if (!string.IsNullOrWhiteSpace(billNumber))
        {
            parameters["billNumber"] = billNumber.Trim();
        }

        if (!string.IsNullOrEmpty(stage))
        {
            parameters["stage"] = stage;
        }

        if (!string.IsNullOrEmpty(outcome))
        {
            parameters["outcome"] = outcome;
        }

        if (sort != "dateDesc")
        {
            parameters["sort"] = sort;
        }

        if (page > 1)
        {
            parameters["page"] = page.ToString(CultureInfo.InvariantCulture);
        }

        for (var i = 0; i < conditions.Count; i++)
        {
            var c = conditions[i];
            var prefix = "factionConditions[" + i.ToString(CultureInfo.InvariantCulture) + "].";
            parameters[prefix + "faction"] = c.Faction;
            parameters[prefix + "result"] = c.Result;
            parameters[prefix + "operator"] = c.Operator;
            parameters[prefix + "threshold"] = c.Threshold.ToString(CultureInfo.InvariantCulture);
            parameters[prefix + "basis"] = c.Basis;
        }

        return QueryHelpers.AddQueryString("/search", parameters);
    }

    private static string Get(Dictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset? ParseDate(string value)
    {
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue))
            : null;
    }
}