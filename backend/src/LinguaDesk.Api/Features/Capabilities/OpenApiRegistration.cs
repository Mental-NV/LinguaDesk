using System.ComponentModel;
using System.Reflection;
using LinguaDesk.Api.Features.Identity.Bearer;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using LinguaDesk.Api.Features.Identity.Session;
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
                    Version = "0.1.0-m009",
                    Description =
                        "Pre-release M009 contract containing implemented public capabilities, local-account registration/verification, browser cookie-session, and independent-client bearer operations. " +
                        "No compatibility or deprecation guarantee is implied.",
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["sessionCookie"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = SessionAuthentication.SessionCookieName,
                    Description = "Secure, HttpOnly, SameSite=Lax, host-scoped nonpersistent browser session cookie.",
                };
                document.Components.SecuritySchemes["antiforgeryHeader"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = SessionAuthentication.AntiforgeryHeaderName,
                    Description = "Opaque request token paired with the Secure, HttpOnly, SameSite=Strict __Host-LinguaDesk.Antiforgery cookie.",
                };
                document.Components.SecuritySchemes["antiforgeryCookie"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = SessionAuthentication.AntiforgeryCookieName,
                    Description = "Secure, HttpOnly, SameSite=Strict host-scoped cookie paired with X-LinguaDesk-Antiforgery.",
                };
                document.Components.SecuritySchemes["bearerAuth"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Description = "Opaque ASP.NET Core Identity bearer access token presented as `Authorization: Bearer ...`. Tokens are never set as cookies or persisted by browser JavaScript.",
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

                    if (property.DeclaringType == typeof(SignInRequest)
                        && string.Equals(property.Name, nameof(SignInRequest.Email), StringComparison.Ordinal))
                    {
                        schema.Format = "email";
                        schema.MaxLength = 254;
                    }

                    if (property.DeclaringType == typeof(SignInRequest)
                        && string.Equals(property.Name, nameof(SignInRequest.Password), StringComparison.Ordinal))
                    {
                        schema.Format = "password";
                        schema.MinLength = 15;
                        schema.MaxLength = 128;
                        schema.WriteOnly = true;
                    }

                    if (property.DeclaringType == typeof(BearerSignInRequest)
                        && string.Equals(property.Name, nameof(BearerSignInRequest.Email), StringComparison.Ordinal))
                    {
                        schema.Format = "email";
                        schema.MaxLength = 254;
                    }

                    if (property.DeclaringType == typeof(BearerSignInRequest)
                        && string.Equals(property.Name, nameof(BearerSignInRequest.Password), StringComparison.Ordinal))
                    {
                        schema.Format = "password";
                        schema.MinLength = 15;
                        schema.MaxLength = 128;
                        schema.WriteOnly = true;
                    }

                    if (property.DeclaringType == typeof(BearerRefreshRequest))
                    {
                        schema.MinLength = 1;
                        schema.WriteOnly = true;
                    }

                    if (property.DeclaringType == typeof(BearerTokenPairResponse))
                    {
                        if (string.Equals(property.Name, nameof(BearerTokenPairResponse.ExpiresIn), StringComparison.Ordinal))
                        {
                            schema.Type = JsonSchemaType.Integer;
                            schema.Pattern = null;
                        }

                        if ((string.Equals(property.Name, nameof(BearerTokenPairResponse.AccessToken), StringComparison.Ordinal)
                                || string.Equals(property.Name, nameof(BearerTokenPairResponse.RefreshToken), StringComparison.Ordinal))
                            && schema.Type is null)
                        {
                            schema.Type = JsonSchemaType.String;
                        }

                        if (string.Equals(property.Name, nameof(BearerTokenPairResponse.AccessToken), StringComparison.Ordinal)
                            || string.Equals(property.Name, nameof(BearerTokenPairResponse.RefreshToken), StringComparison.Ordinal))
                        {
                            schema.WriteOnly = true;
                        }
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

                if (endpointName is "getAccountAntiforgeryToken" or "signInLocalAccount"
                    or "getLocalAccountSession" or "signOutLocalAccount"
                    or "signInBearerClient" or "refreshBearerClient" or "getCurrentAccount")
                {
                    if (operation.Responses is not null)
                    {
                        foreach (var accountResponse in operation.Responses.Values.OfType<OpenApiResponse>())
                        {
                            accountResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
                            accountResponse.Headers["Cache-Control"] = new OpenApiHeader
                            {
                                Description = "Always `no-store` for account-session responses.",
                                Required = true,
                                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                            };
                        }
                    }

                    if (endpointName is "getAccountAntiforgeryToken" or "signInLocalAccount" or "signOutLocalAccount")
                    {
                        var successStatus = endpointName == "signOutLocalAccount" ? "204" : "200";
                        if (operation.Responses?.TryGetValue(successStatus, out var success) == true
                            && success is OpenApiResponse successResponse)
                        {
                            successResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
                            successResponse.Headers["Set-Cookie"] = new OpenApiHeader
                            {
                                Description = endpointName == "getAccountAntiforgeryToken"
                                    ? "Sets only the Secure, HttpOnly, SameSite=Strict, Path=/ __Host-LinguaDesk.Antiforgery cookie."
                                    : endpointName == "signInLocalAccount"
                                        ? "Sets only the Secure, HttpOnly, SameSite=Lax, Path=/ nonpersistent __Host-LinguaDesk.Session cookie."
                                        : "Expires only the Path=/ __Host-LinguaDesk.Session cookie.",
                                Required = true,
                                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                            };
                        }
                    }

                    if (endpointName is "signInLocalAccount" or "signOutLocalAccount")
                    {
                        operation.Security =
                        [
                            new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("antiforgeryHeader", context.Document)] = [],
                                [new OpenApiSecuritySchemeReference("antiforgeryCookie", context.Document)] = [],
                            },
                        ];
                    }
                    else if (endpointName == "getLocalAccountSession")
                    {
                        operation.Security =
                        [
                            new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("sessionCookie", context.Document)] = [],
                            },
                        ];
                    }
                    else if (endpointName == "getCurrentAccount")
                    {
                        operation.Security =
                        [
                            new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("bearerAuth", context.Document)] = [],
                            },
                            new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("sessionCookie", context.Document)] = [],
                            },
                        ];
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
