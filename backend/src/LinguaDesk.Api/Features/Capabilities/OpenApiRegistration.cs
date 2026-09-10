using System.ComponentModel;
using System.Reflection;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
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
                    Version = "0.1.0-m007",
                    Description =
                        "Pre-release M007 contract containing only the implemented public capabilities, anonymous local-account registration, and email-verification operations. " +
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

                    if (property.DeclaringType == typeof(RegistrationRequest)
                        && string.Equals(property.Name, nameof(RegistrationRequest.Email), StringComparison.Ordinal))
                    {
                        schema.Format = "email";
                        schema.MaxLength = 254;
                    }

                    if (property.DeclaringType == typeof(RegistrationRequest)
                        && string.Equals(property.Name, nameof(RegistrationRequest.Password), StringComparison.Ordinal))
                    {
                        schema.Format = "password";
                        schema.MinLength = 15;
                        schema.MaxLength = 128;
                        schema.WriteOnly = true;
                    }

                    if (property.DeclaringType == typeof(ConfirmEmailRequest)
                        && string.Equals(property.Name, nameof(ConfirmEmailRequest.UserId), StringComparison.Ordinal))
                    {
                        schema.MinLength = 1;
                        schema.MaxLength = 450;
                    }

                    if (property.DeclaringType == typeof(ConfirmEmailRequest)
                        && string.Equals(property.Name, nameof(ConfirmEmailRequest.Code), StringComparison.Ordinal))
                    {
                        schema.MinLength = 1;
                        schema.MaxLength = 4096;
                        schema.WriteOnly = true;
                    }

                    if (property.DeclaringType == typeof(ResendVerificationRequest)
                        && string.Equals(property.Name, nameof(ResendVerificationRequest.Email), StringComparison.Ordinal))
                    {
                        schema.Format = "email";
                        schema.MaxLength = 254;
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

                if ((string.Equals(endpointName, "registerLocalAccount", StringComparison.Ordinal)
                        || string.Equals(endpointName, "confirmLocalAccountEmail", StringComparison.Ordinal)
                        || string.Equals(endpointName, "resendLocalAccountVerification", StringComparison.Ordinal))
                    && operation.Responses is not null)
                {
                    foreach (var registrationResponse in operation.Responses.Values.OfType<OpenApiResponse>())
                    {
                        registrationResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
                        registrationResponse.Headers["Cache-Control"] = new OpenApiHeader
                        {
                            Description = "Always `no-store` for local-account mutation responses.",
                            Required = true,
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                        };
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
