using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Расширения HTTP-клиента для интеграционных тестов API
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Выполняет GET-запрос, проверяет код 200 и возвращает корень JSON-ответа
    /// </summary>
    public static async Task<JsonElement> GetJsonAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "запрос {0} должен завершиться успешно", url);
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return document!.RootElement;
    }
}