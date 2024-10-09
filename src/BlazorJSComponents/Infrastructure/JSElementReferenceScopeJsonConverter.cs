using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorJSComponents;

internal sealed class JSElementReferenceScopeJsonConverter : JsonConverter<JSElementReferenceScope>
{
    private static readonly JsonEncodedText s_idProperty = JsonEncodedText.Encode("__jsScope");

    public override JSElementReferenceScope? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new InvalidOperationException($"{nameof(JSElementReferenceScope)} deserialization is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, JSElementReferenceScope value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(s_idProperty, value._id);
        writer.WriteEndObject();
    }
}
