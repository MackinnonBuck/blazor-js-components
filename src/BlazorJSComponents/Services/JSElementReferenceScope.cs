using System.Text.Json.Serialization;

namespace BlazorJSComponents;

/// <summary>
/// Represents a scope for element references to be passed to JavaScript components.
/// </summary>
/// <remarks>
/// Typical usage is to inject a <see cref="JSElementReferenceScope"/> instance into a component,
/// then use its indexer to specify element reference parameter names:
/// <example>
/// <code lang='razor'>
/// @inject JSElementReferenceScope Refs
///
/// &lt;input type="text" ... data-ref="@Refs["textInput"]" /&gt;
///
/// &lt;JS ... Args="[Refs]" /&gt;
/// </code>
/// </example>
/// You can then directly access the referenced elements in the JavaScript component:
/// <example>
/// <code lang='javascript'>
/// export default class extends BlazorJSComponents.Component {
///   setParameters({ textInput }) {
///     this.setEventListener(textInput, 'input', ...);
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[JsonConverter(typeof(JSElementReferenceScopeJsonConverter))]
public sealed class JSElementReferenceScope
{
    internal readonly string _id;

    /// <summary>
    /// Creates a new <see cref="JSElementReferenceScope"/>.
    /// </summary>
    public static JSElementReferenceScope Create()
        => new(Guid.CreateVersion7().ToString("N"));

    internal JSElementReferenceScope(string id)
    {
        _id = id;
    }

    /// <summary>
    /// Given a JS property name, returns a unique identifier for the specified element.
    /// </summary>
    public string this[string name]
        => $"{_id}-{name}";
}
