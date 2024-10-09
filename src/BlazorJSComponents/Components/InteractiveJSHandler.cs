using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace BlazorJSComponents;

internal sealed class InteractiveJSHandler(string src, string? key, IJSRuntime jsRuntime) : IJSHandler
{
    private int _instanceId;
    private TaskCompletionSource? _initTcs = new();
    private Task? _onAfterRenderTask;
    private object?[]? _args;

    public void SetArgs(object?[]? args)
        => _args = args;

    public void Render(RenderTreeBuilder builder)
    {
        // No content to render
    }

    public async Task OnAfterRenderAsync()
    {
        _onAfterRenderTask = OnAfterRenderAsyncCore();
        try
        {
            await _onAfterRenderTask;
        }
        finally
        {
            _onAfterRenderTask = null;
        }

        async Task OnAfterRenderAsyncCore()
        {
            if (_initTcs is not null)
            {
                _instanceId = await jsRuntime.InvokeAsync<int>("__blazorScript.getOrCreateJSComponent", 0, src, key);
                _initTcs.SetResult();
                _initTcs = null;
            }

            if (_instanceId != 0)
            {
                await jsRuntime.InvokeVoidAsync("__blazorScript.setJSComponentParameters", _instanceId, _args);
            }
        }
    }

    public async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args)
    {
        await WaitForPendingRenderAsync();

        if (_instanceId == 0)
        {
            throw new InvalidOperationException($"There is no JS component associated with the '{nameof(JS)}' instance");
        }

        return await jsRuntime.InvokeAsync<TValue>("__blazorScript.invokeJSComponentMethod", _instanceId, identifier, args);
    }

    public async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        await WaitForPendingRenderAsync();

        if (_instanceId == 0)
        {
            throw new InvalidOperationException($"There is no JS component associated with the '{nameof(JS)}' instance");
        }

        return await jsRuntime.InvokeAsync<TValue>("__blazorScript.invokeJSComponentMethod", cancellationToken, _instanceId, identifier, args);
    }

    private async Task WaitForPendingRenderAsync()
    {
        if (_initTcs is not null)
        {
            await _initTcs.Task;
        }

        if (_onAfterRenderTask is not null)
        {
            await _onAfterRenderTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_instanceId != 0)
        {
            try
            {
                await WaitForPendingRenderAsync();
                await jsRuntime.InvokeVoidAsync("__blazorScript.disposeJSComponent", _instanceId);
            }
            catch (JSDisconnectedException)
            {
                // Ignore
            }
        }
    }
}
