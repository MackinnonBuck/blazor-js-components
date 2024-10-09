using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;

namespace BlazorJSComponents;

internal sealed class JSComponentManager
{
    private readonly ConcurrentDictionary<Type, string> _componentCollocatedJSPathCache = [];
    private readonly ConcurrentDictionary<Assembly, AssemblyCollocatedJSAttribute?> _assemblyCollocatedJSAttributeCache = [];

    // TODO: Make these options configurable.
    public JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
    };

    public JSComponentManager()
    {
        if (MetadataUpdater.IsSupported)
        {
            BlazorJSComponentsMetadataUpdateHandler.TrackJSComponentManager(this);
        }
    }

    public string GetCollocatedComponentJSPath(IComponent component)
    {
        var type = component.GetType();
        return _componentCollocatedJSPathCache.GetOrAdd(type, ComputeCollocatedComponentJSPath);
    }

    private string ComputeCollocatedComponentJSPath(Type type)
    {
        var assembly = type.Assembly;
        var assemblyCollocatedJS = _assemblyCollocatedJSAttributeCache.GetOrAdd(
            assembly,
            static assembly => assembly.GetCustomAttribute<AssemblyCollocatedJSAttribute>())
            ?? throw new InvalidOperationException(
                $"The assembly for component of type '{type.FullName}' is not annotated with " +
                $"'{nameof(AssemblyCollocatedJSAttribute)}'. This is required " +
                $"in order to automatically determine the collocated JS path.");

        var componentCollocatedJS = type.GetCustomAttribute<DiscoverCollocatedJSAttribute>()
            ?? throw new InvalidOperationException(
                $"The component of type '{type.FullName}' must be annotated with " +
                $"'{nameof(DiscoverCollocatedJSAttribute)}' in order to automatically infer the collocated " +
                $"JS file path.");

        var razorFilePath = componentCollocatedJS.RazorFilePath
            ?? throw new InvalidOperationException(
                $"The '{nameof(DiscoverCollocatedJSAttribute)}' on component type " +
                $"'{type.FullName}' did not specify a valid razor file path.");

        var pathPrefix = assemblyCollocatedJS.CallerFileNamePathPrefix;
        if (!razorFilePath.StartsWith(pathPrefix))
        {
            throw new InvalidOperationException(
                $"Expected the razor file path '{razorFilePath}' to start with the computed " +
                $"assembly file path prefix '{pathPrefix}'.");
        }

        var relativeRazorFilePath = razorFilePath[pathPrefix.Length..];
        var collocatedJSFilePath = $"./{assemblyCollocatedJS.StaticWebAssetBasePath}{relativeRazorFilePath}.js";
        return collocatedJSFilePath;
    }

    internal void ClearCache(Type[]? updatedTypes)
    {
        if (updatedTypes is null)
        {
            _componentCollocatedJSPathCache.Clear();
            _assemblyCollocatedJSAttributeCache.Clear();
        }
        else
        {
            foreach (var type in updatedTypes)
            {
                _componentCollocatedJSPathCache.TryRemove(type, out _);
            }
        }
    }
}
