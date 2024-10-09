using System.ComponentModel;

namespace BlazorJSComponents;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class AssemblyCollocatedJSAttribute(string callerFileNamePathPrefix, string staticWebAssetBasePath) : Attribute
{
    public string CallerFileNamePathPrefix { get; } = callerFileNamePathPrefix;

    public string StaticWebAssetBasePath { get; } = staticWebAssetBasePath;
}
