using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace BlazorJSComponents;

internal class StaticJSHandler(
    string src,
    string? key,
    bool mayBecomeInteractive,
    UniqueIdAllocator uniqueIdAllocator,
    JsonSerializerOptions jsonSerializerOptions) : IJSHandler
{
    private object?[]? _args;

    public void SetArgs(object?[]? args)
        => _args = args;

    public void Render(RenderTreeBuilder builder)
    {
        var renderId = uniqueIdAllocator.GetNextId();

        if (_args is { Length: > 0 })
        {
            var argsJson = JsonSerializer.Serialize(_args, jsonSerializerOptions);
            builder.OpenElement(0, "script");
            builder.AddAttribute(1, "id", $"bl-args-{renderId}");
            builder.AddAttribute(2, "type", "application/json");
            builder.AddMarkupContent(3, argsJson);
            builder.CloseElement();
        }

        builder.OpenElement(4, "bl-script");
        builder.AddAttribute(5, "src", src);
        builder.AddAttribute(6, "key", key);
        builder.AddAttribute(7, "int", mayBecomeInteractive);
        builder.AddAttribute(8, "inst", renderId);
        builder.CloseElement();
    }

    public Task OnAfterRenderAsync()
        => Task.CompletedTask;

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args)
        => throw CannotInvokeJSDuringStaticRendering();

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw CannotInvokeJSDuringStaticRendering();

    private static InvalidOperationException CannotInvokeJSDuringStaticRendering()
        => new("JavaScript interop calls cannot be issued during static rendering");

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
