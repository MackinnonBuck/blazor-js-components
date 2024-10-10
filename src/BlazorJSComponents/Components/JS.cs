using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BlazorJSComponents;

/// <summary>
/// Represents some JavaScript to dynamically import. This may or may not refer to a JavaScript component.
/// </summary>
public sealed class JS : IComponent, IHandleAfterRender, IJSObjectReference, IAsyncDisposable
{
    private RenderHandle _renderHandle;
    private IJSHandler? _jsHandler;

    [Inject]
    private JSComponentManager JSComponentManager { get; set; } = default!;

    [Inject]
    private UniqueIdAllocator UniqueIdAllocator { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private IJSHandler NotNullJSHandler
        => _jsHandler ?? throw new InvalidOperationException(
            $"This operation is not permitted until parameters are set on the {nameof(JS)} instance.");

    /// <summary>
    /// Gets or sets path to the JavaScript file to load.
    /// </summary>
    /// <remarks>
    /// If <see cref="For"/> is specified, this property must be <c>null</c>.
    /// </remarks>
    [Parameter]
    public string? Src { get; set; }

    /// <summary>
    /// Gets or sets the component instance whose collocated JS file should be loaded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The declaration for the type of the target component must include a <see cref="DiscoverCollocatedJSAttribute"/> attribute
    /// in its <c>.razor</c> file.
    /// </para>
    /// <para>
    /// If <see cref="Src"/> is specified, this property must be <c>null</c>.
    /// </para>
    /// </remarks>
    [Parameter]
    public IComponent? For { get; set; }

    /// <summary>
    /// Gets or sets the unique key used to preserve the JavaScript component instance across
    /// enhanced page updates or during the transition to .NET interactivity.
    /// </summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the arguments to pass to the JavaScript component.
    /// </summary>
    [Parameter]
    public object?[]? Args { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
        => _renderHandle = renderHandle;

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        var (oldSrc, oldFor, oldKey) = (Src, For, Key);
        (Src, For, Key, Args) = ExtractParameters(in parameters);

        if (_jsHandler is null)
        {
            // If this is the first time parameters are being set, we need to validate them
            // and initialize the handler.
            var src = (Src, For) switch
            {
                (Src: not null, For: null)      => Src,
                (Src: null,     For: not null)  => JSComponentManager.GetCollocatedComponentJSPath(For),
                (Src: null,     For: null)      => throw MustSpecifyEitherSrcOrFor(),
                (Src: not null, For: not null)  => throw CannotSpecifyBothSrcAndFor(),
            };

            _jsHandler = _renderHandle.RendererInfo.IsInteractive
                ? new InteractiveJSHandler(
                    src,
                    Key,
                    JSRuntime)
                : new StaticJSHandler(
                    src,
                    Key,
                    mayBecomeInteractive: _renderHandle.RenderMode is not null,
                    UniqueIdAllocator,
                    jsonSerializerOptions: JSComponentManager.JsonSerializerOptions);

            static InvalidOperationException MustSpecifyEitherSrcOrFor()
                => new($"Must specify eitehr '{nameof(Src)}' or '{nameof(For)}'.");

            static InvalidOperationException CannotSpecifyBothSrcAndFor()
                => new($"Must specify one of '{nameof(Src)}' or '{nameof(For)}', but not both.");
        }
        else
        {
            // Since we've already initialized the handler, all we need to do is validate
            // that certain arguments have not changed.
            ThrowIfChanged(oldSrc, Src);
            ThrowIfChanged(oldFor, For);
            ThrowIfChanged(oldKey, Key);
        }

        _jsHandler.SetArgs(Args);
        _renderHandle.Render(_jsHandler.Render);
        return Task.CompletedTask;
    }

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier, object?[]? args)
        => NotNullJSHandler.InvokeAsync<TValue>(identifier, args);

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier, CancellationToken cancellationToken, object?[]? args)
        => NotNullJSHandler.InvokeAsync<TValue>(identifier, args);

    Task IHandleAfterRender.OnAfterRenderAsync()
        => NotNullJSHandler.OnAfterRenderAsync();

    ValueTask IAsyncDisposable.DisposeAsync()
        => _jsHandler?.DisposeAsync() ?? ValueTask.CompletedTask;

    private static (string? Src, IComponent? For, string? Key, object?[]? Args) ExtractParameters(in ParameterView parameters)
    {
        string? src = null;
        IComponent? @for = null;
        string? key = null;
        object?[]? args = null;

        foreach (var parameter in parameters)
        {
            var name = parameter.Name;

            if (string.Equals(name, nameof(Src), StringComparison.Ordinal))
            {
                src = parameter.Value as string;
            }
            else if (string.Equals(name, nameof(For), StringComparison.Ordinal))
            {
                @for = parameter.Value as IComponent;
            }
            else if (string.Equals(name, nameof(Key), StringComparison.Ordinal))
            {
                key = parameter.Value as string;
            }
            else if (string.Equals(name, nameof(Args), StringComparison.Ordinal))
            {
                args = parameter.Value as object?[];
            }
            else
            {
                throw new InvalidOperationException($"Unexpected {nameof(JS)} parameter '{name}'.");
            }
        }

        return (Src: src, For: @for, Key: key, Args: args);
    }

    private static void ThrowIfChanged(
        object? oldValue,
        object? currentValue,
        [CallerArgumentExpression(nameof(currentValue))] string? paramName = null)
    {
        if (!Equals(oldValue, currentValue))
        {
            throw new InvalidOperationException($"Cannot dynamically change the value of the '{paramName}' parameter.");
        }
    }
}
