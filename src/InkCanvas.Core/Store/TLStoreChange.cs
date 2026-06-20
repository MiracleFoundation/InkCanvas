namespace InkCanvas.Core.Store;

/// <summary>Event data for a single store change.</summary>
public record TLStoreChange(TLStoreChange.ChangeType Type, string RecordId, TLRecord? Record)
{
    public enum ChangeType { Put, Remove }
}
