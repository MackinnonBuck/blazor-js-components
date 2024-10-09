using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BlazorJSComponents.Tasks;

public sealed class ComputeCallerFileNamePrefix : Task
{
    [Required]
    public ITaskItem[] SourceRoot { get; set; } = default!;

    [Required]
    public string ProjectDir { get; set; } = default!;

    [Output]
    public string? CallerFileNamePrefix { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Low, $"Creating path map:");

        var pathMapBuilder = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();

        foreach (var sourceRoot in SourceRoot)
        {
            var root = sourceRoot.ItemSpec;
            var mappedPath = sourceRoot.GetMetadata("MappedPath");

            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(mappedPath))
            {
                Log.LogMessage(MessageImportance.Low, $"  Skipping source root '{root}' with mapped path '{mappedPath}' because either the key or value was empty.");
            }

            Log.LogMessage(MessageImportance.Low, $"  Adding source root '{root}' with mapped path '{mappedPath}'.");
            pathMapBuilder.Add(new(root, mappedPath));
        }

        var pathMap = pathMapBuilder.ToImmutable();
        var resolver = new SourceFileResolver([], null, pathMap);
        CallerFileNamePrefix = resolver.NormalizePath(ProjectDir, baseFilePath: null);
        return true;
    }
}
