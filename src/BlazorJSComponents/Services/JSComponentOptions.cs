using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BlazorJSComponents;

/// <summary>
/// Options for configuring JavaScript components.
/// </summary>
public sealed class JSComponentOptions
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used to serialize <see cref="JS"/> arguments
    /// during static rendering.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? new DefaultJsonTypeInfoResolver()
            : JsonTypeInfoResolver.Combine(),
    };
}
