using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi;

namespace LinguaDesk.Api.Features.Operations;

/// <summary>
/// Restores the translation 201 envelope component dropped by minimal-API
/// response collapsing: duplicate (status code, content type) response
/// metadata keeps only the last declaration, so the translation success
/// schema never reaches generation while the operation transformer
/// references both variants with anyOf. The two envelopes share every
/// property except the result-text field, so the translation component is
/// cloned from the live generated rewriting component with only that
/// property renamed. Symmetry is guarded by
/// RewriteAndTranslationSuccessEnvelopesStaySymmetric: diverging the DTOs
/// requires updating this clone.
/// </summary>
public static class OperationsSuccessSchemas
{
    public static IOpenApiSchema CloneRewritingAsTranslation(IOpenApiSchema rewriting)
    {
        ArgumentNullException.ThrowIfNull(rewriting);
        if (rewriting.Properties is null
            || !rewriting.Properties.TryGetValue("rewrittenText", out var textSchema)
            || textSchema is null)
        {
            throw new InvalidOperationException("The generated rewriting envelope must carry a rewrittenText property.");
        }

        var translatedTextDescription = typeof(TranslationSuccessResponse)
            .GetProperty(nameof(TranslationSuccessResponse.TranslatedText))?
            .GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (string.IsNullOrEmpty(translatedTextDescription))
        {
            throw new InvalidOperationException("TranslationSuccessResponse.TranslatedText must carry a Description.");
        }

        var properties = new Dictionary<string, IOpenApiSchema>(rewriting.Properties, StringComparer.Ordinal);
        properties.Remove("rewrittenText");
        properties["translatedText"] = new OpenApiSchema
        {
            Type = textSchema.Type,
            Format = textSchema.Format,
            Description = translatedTextDescription,
        };

        var required = new HashSet<string>(StringComparer.Ordinal);
        if (rewriting.Required is not null)
        {
            foreach (var name in rewriting.Required)
            {
                required.Add(name);
            }
        }

        required.Remove("rewrittenText");
        required.Add("translatedText");

        return new OpenApiSchema
        {
            Type = rewriting.Type,
            Description = rewriting.Description,
            Properties = properties,
            Required = required,
        };
    }
}
