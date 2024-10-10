using System.Runtime.CompilerServices;

namespace BlazorJSComponents;

/// <summary>
/// Indicates that the Razor component has a collocated JavaScript file that should be discoverable.
/// </summary>
/// <remarks>
/// This attribute must only be used in <c>.razor</c> files that represent a Razor component.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DiscoverCollocatedJSAttribute([CallerFilePath] string? razorFilePath = null) : Attribute
{
    public string? RazorFilePath { get; } = razorFilePath;
}
