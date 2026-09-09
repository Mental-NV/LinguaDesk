using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LinguaDesk.Api.Features.Capabilities;

public static class OpenApiRegistration
{
    public static IServiceCollection AddLinguaDeskOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(CapabilitiesEndpoints.OpenApiDocumentName, options =>
        {
            options.ShouldInclude = static description =>
                string.Equals(
                    description.GroupName,
                    CapabilitiesEndpoints.OpenApiDocumentName,
                    StringComparison.Ordinal);
            options.AddDocumentTransformer(static (document, _, _) =>
            {
                document.Info = new()
                {
                    Title = "LinguaDesk API",
                    Version = "0.1.0-m005",
                    Description =
                        "Pre-release M005 contract containing only the implemented public capabilities operation. " +
                        "No compatibility or deprecation guarantee is implied.",
                };
                return Task.CompletedTask;
            });
            options.AddSchemaTransformer(static (schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type.IsEnum && schema.Type is null)
                {
                    schema.Type = JsonSchemaType.String;
                }

                if (context.JsonPropertyInfo?.AttributeProvider is PropertyInfo property)
                {
                    if (property.GetCustomAttribute<DescriptionAttribute>() is { } description)
                    {
                        schema.Description = description.Description;
                    }

                    if (property.PropertyType == typeof(int))
                    {
                        schema.Type = JsonSchemaType.Integer;
                        schema.Pattern = null;
                    }
                }

                return Task.CompletedTask;
            });
            options.AddOperationTransformer(static (operation, context, _) =>
            {
                var endpointName = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<IEndpointNameMetadata>()
                    .SingleOrDefault()?.EndpointName;
                if (string.Equals(endpointName, "getCapabilities", StringComparison.Ordinal)
                    && operation.Responses is not null
                    && operation.Responses.TryGetValue("200", out var response)
                    && response is OpenApiResponse concreteResponse)
                {
                    concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
                    concreteResponse.Headers["Cache-Control"] = new OpenApiHeader
                    {
                        Description = "Always `no-store` because the response includes current server time.",
                        Required = true,
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                        },
                    };
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
