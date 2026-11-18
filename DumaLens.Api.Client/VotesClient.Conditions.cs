using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace DumaLens.Api.Client;

/// <summary>
/// Расширение клиента голосований: корректная передача фракционных условий
/// </summary>
public partial interface IVotesClient
{
    /// <summary>
    /// Поиск голосований с индексной сериализацией фракционных условий (FactionConditions[i].*)
    /// </summary>
    /// <param name="q">Текстовый запрос</param>
    /// <param name="convocation">Номер созыва</param>
    /// <param name="dateFrom">Начало диапазона дат</param>
    /// <param name="dateTo">Конец диапазона дат</param>
    /// <param name="billNumber">Номер законопроекта</param>
    /// <param name="stage">Стадия (токен имени члена)</param>
    /// <param name="outcome">Итог (accepted/rejected/unknown)</param>
    /// <param name="sort">Сортировка (dateDesc/dateAsc/relevance/acceptCountDesc)</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="factionConditions">Фракционные условия (комбинируются И)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Страница результатов поиска</returns>
    Task<VoteSearchResultApiModel> SearchWithConditionsAsync(string? q, int? convocation, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, string? billNumber, string? stage, string? outcome, string? sort, int? page, int? pageSize, IEnumerable<FactionConditionRequestApiModel>? factionConditions, CancellationToken cancellationToken = default);
}

/// <summary>
/// Реализация поиска с корректной индексной сериализацией фракционных условий
/// </summary>
public partial class VotesClient
{
    /// <summary>
    /// Поиск голосований: фракционные условия передаются индексными ключами FactionConditions[i].*
    /// (сгенерированный SearchAsync сериализует условия через ToString, что API не связывает)
    /// </summary>
    public async Task<VoteSearchResultApiModel> SearchWithConditionsAsync(string? q, int? convocation, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, string? billNumber, string? stage, string? outcome, string? sort, int? page, int? pageSize, IEnumerable<FactionConditionRequestApiModel>? factionConditions, CancellationToken cancellationToken = default)
    {
        var urlBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl))
        {
            urlBuilder.Append(_baseUrl);
        }

        urlBuilder.Append("api/Votes?");
        AppendParam(urlBuilder, "Q", q);
        if (convocation.HasValue)
        {
            AppendParam(urlBuilder, "Convocation", convocation.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (dateFrom.HasValue)
        {
            AppendParam(urlBuilder, "DateFrom", dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (dateTo.HasValue)
        {
            AppendParam(urlBuilder, "DateTo", dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        AppendParam(urlBuilder, "BillNumber", billNumber);
        AppendParam(urlBuilder, "Stage", stage);
        AppendParam(urlBuilder, "Outcome", outcome);
        AppendParam(urlBuilder, "Sort", sort);
        if (page.HasValue)
        {
            AppendParam(urlBuilder, "Page", page.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (pageSize.HasValue)
        {
            AppendParam(urlBuilder, "PageSize", pageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (factionConditions != null)
        {
            var index = 0;
            foreach (var condition in factionConditions)
            {
                var prefix = "FactionConditions[" + index.ToString(CultureInfo.InvariantCulture) + "].";
                AppendParam(urlBuilder, prefix + "Faction", condition.Faction);
                AppendParam(urlBuilder, prefix + "Result", condition.Result);
                AppendParam(urlBuilder, prefix + "Operator", condition.Operator);
                AppendParam(urlBuilder, prefix + "Threshold", condition.Threshold.ToString(CultureInfo.InvariantCulture));
                AppendParam(urlBuilder, prefix + "Basis", condition.Basis);
                index++;
            }
        }

        urlBuilder.Length--;
        var url = urlBuilder.ToString();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.RelativeOrAbsolute));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        await PrepareRequestAsync(_httpClient, request, url, cancellationToken).ConfigureAwait(false);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        try
        {
            await ProcessResponseAsync(_httpClient, response, cancellationToken).ConfigureAwait(false);
            var headers = new Dictionary<string, IEnumerable<string>>();
            foreach (var header in response.Headers)
            {
                headers[header.Key] = header.Value;
            }

            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    headers[header.Key] = header.Value;
                }
            }

            var status = (int)response.StatusCode;
            if (status == 200)
            {
                var objectResponse = await ReadObjectResponseAsync<VoteSearchResultApiModel>(response, headers, cancellationToken).ConfigureAwait(false);
                return objectResponse.Object ?? throw new ApiException("Response was null which was not expected.", status, objectResponse.Text, headers, null);
            }

            if (status == 422)
            {
                var objectResponse = await ReadObjectResponseAsync<ApiValidationExceptionDetail>(response, headers, cancellationToken).ConfigureAwait(false);
                throw new ApiException<ApiValidationExceptionDetail>("Unprocessable Content", status, objectResponse.Text, headers, objectResponse.Object, null);
            }

            var responseData = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status.ToString(CultureInfo.InvariantCulture) + ").", status, responseData, headers, null);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static void AppendParam(StringBuilder builder, string key, string? value)
    {
        if (value != null)
        {
            builder.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value)).Append('&');
        }
    }
}