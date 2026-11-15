using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DumaLens.Api.Infrastructure;

/// <summary>
/// Генерация имён Enum в swagger
/// </summary>
public class EnumNameSchemaFilter : ISchemaFilter
{
    void ISchemaFilter.Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
        {
            return;
        }
        // Ссылка на enum-схему или её allOf-обёртка (UseAllOfToExtendReferenceSchemas):
        // тип и значения уже заданы в компонентной схеме; дублирование здесь порождает
        // отдельные enum-типы на каждое DTO в NSwag-клиенте вместо общих VoteStage и т.п.
        if (schema.Reference != null || schema.AllOf?.Count > 0)
        {
            return;
        }
        schema.Type = "string";
        schema.Enum = Enum.GetNames(context.Type)
            .Select(name => new OpenApiString(name) as IOpenApiAny)
            .ToList();
        schema.Format = null;
    }
}