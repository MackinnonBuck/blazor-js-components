namespace BlazorJSComponents;

// Allocates unique identifiers for embedding information in the DOM.
// Uses a GUID to guarantee uniqueness between DI scopes, then a simple incrementing integer
// for uniqueness within a DI scope.
internal sealed class UniqueIdAllocator
{
    private readonly string _scopeId = Guid.CreateVersion7().ToString("N");

    private int _nextInstanceId = 1;

    public string GetNextId()
        => $"{_scopeId}:{_nextInstanceId++}";
}
