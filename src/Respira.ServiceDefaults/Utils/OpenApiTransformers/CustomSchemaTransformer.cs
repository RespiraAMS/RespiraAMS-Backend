using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Respira.ServiceDefaults.Utils.OpenApiTransformers;

public sealed class CustomSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Targets your enum
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = Enum
                .GetNames(context.JsonTypeInfo.Type)
                .Select(JsonNode (name) => JsonValue.Create(name))
                .ToList();
        }

        return Task.CompletedTask;
    }
}