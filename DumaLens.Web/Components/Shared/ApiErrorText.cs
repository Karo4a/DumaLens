using DumaLens.Api.Client;
using System.Text.Json;

namespace DumaLens.Web.Components.Shared;

/// <summary>
/// Текст ошибки Api для интерфейса: сообщение сервера либо человекочитаемый статус
/// </summary>
public static class ApiErrorText
{
    /// <summary>
    /// Описывает ошибку NSwag-клиента по статусу и телу ответа
    /// </summary>
    public static string Describe(ApiException exception)
    {
        var message = TryReadMessage(exception.Response);
        return exception.StatusCode switch
        {
            403 => "Недостаточно прав для этого действия",
            429 => "Слишком много запросов. Повторите позже",
            _ => message ?? $"Ошибка API ({exception.StatusCode})",
        };
    }

    private static string? TryReadMessage(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var joined = string.Join("; ", errors.EnumerateArray()
                    .Select(error => error.TryGetProperty("message", out var item) ? item.GetString() : null)
                    .Where(text => !string.IsNullOrEmpty(text)));
                return joined.Length > 0 ? joined : null;
            }
        }
        catch (JsonException)
        {
            // Тело ответа не JSON — используем статус
        }
        return null;
    }
}