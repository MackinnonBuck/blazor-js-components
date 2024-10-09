using BlazorJSComponents;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

[assembly: MetadataUpdateHandler(typeof(BlazorJSComponentsMetadataUpdateHandler))]

namespace BlazorJSComponents;

internal static class BlazorJSComponentsMetadataUpdateHandler
{
    private static readonly ConditionalWeakTable<JSComponentManager, object?> s_collocatedJSManagers = [];

    public static void TrackJSComponentManager(JSComponentManager instance)
    {
        s_collocatedJSManagers.Add(instance, null);
    }

    public static void ClearCache(Type[]? types)
    {
        foreach (var (instance, _) in s_collocatedJSManagers)
        {
            instance.ClearCache(types);
        }
    }
}
