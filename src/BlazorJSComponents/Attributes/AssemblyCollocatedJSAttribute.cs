using System.ComponentModel;

namespace BlazorJSComponents;

/// <summary>
/// Not for use by application code.
/// </summary>
/// <remarks>
/// This attribute gets placed on the assembly at build time to include information necessary for deriving
/// paths to collocated JS files.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class AssemblyCollocatedJSAttribute(string callerFileNamePathPrefix, string staticWebAssetBasePath) : Attribute
{
    public string CallerFileNamePathPrefix { get; } = callerFileNamePathPrefix;

    public string StaticWebAssetBasePath { get; } = staticWebAssetBasePath;
}
