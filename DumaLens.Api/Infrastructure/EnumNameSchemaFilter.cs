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
        schema.Type = "string";
        schema.Enum = Enum.GetNames(context.Type)
            .Select(name => new OpenApiString(name) as IOpenApiAny)
            .ToList();
        schema.Format = null;
    }
}