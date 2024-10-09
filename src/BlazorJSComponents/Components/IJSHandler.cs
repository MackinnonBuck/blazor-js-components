using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorJSComponents;

internal interface IJSHandler : IHandleAfterRender, IJSObjectReference
{
    void SetArgs(object?[]? args);

    void Render(RenderTreeBuilder builder);
}
