using System.Text.Json;

namespace Tldraw.Blazor.Core.Store;

internal static class SnapshotJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new SKPointJsonConverter(),
            new SKPointListJsonConverter(),
        },
    };
}
