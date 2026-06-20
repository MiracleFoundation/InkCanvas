namespace Tldraw.Blazor.Core.Store;

/// <summary>Serializable snapshot of the entire store.</summary>
public class TLStoreSnapshot
{
    public List<TLRecord> Records { get; set; } = new();

    public string ToJson() =>
        System.Text.Json.JsonSerializer.Serialize(this, SnapshotJsonOptions.Default);

    public static TLStoreSnapshot? FromJson(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<TLStoreSnapshot>(json, SnapshotJsonOptions.Default);
}
