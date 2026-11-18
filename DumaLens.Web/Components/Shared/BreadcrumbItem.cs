namespace DumaLens.Web.Components.Shared;

/// <summary>
/// Элемент хлебных крошек: текст и необязательная ссылка (null — текущая страница)
/// </summary>
/// <param name="Text">Отображаемый текст</param>
/// <param name="Href">Ссылка перехода; null для текущего раздела</param>
public sealed record BreadcrumbItem(string Text, string? Href = null);