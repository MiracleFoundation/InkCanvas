namespace Tldraw.Blazor.Core.Store;

/// <summary>
/// Reactive record store. All editor state lives here.
/// Emits change events when records are added, updated, or removed.
/// </summary>
public class TLStore
{
    private readonly Dictionary<string, TLRecord> _records = new();

    /// <summary>Fired when records change. Carries the list of changes.</summary>
    public event Action<IReadOnlyList<TLStoreChange>>? Changed;


    /// <summary>Insert or update a record.</summary>
    public void Put(TLRecord record)
    {
        _records[record.Id] = record;
        OnChanged(new[] { new TLStoreChange(TLStoreChange.ChangeType.Put, record.Id, record) });
    }

    /// <summary>Insert or update multiple records.</summary>
    public void PutAll(IEnumerable<TLRecord> records)
    {
        var changes = new List<TLStoreChange>();
        foreach (var record in records)
        {
            _records[record.Id] = record;
            changes.Add(new TLStoreChange(TLStoreChange.ChangeType.Put, record.Id, record));
        }
        if (changes.Count > 0)
            OnChanged(changes);
    }

    /// <summary>Remove a record by ID.</summary>
    public bool Remove(string id)
    {
        if (_records.Remove(id))
        {
            OnChanged(new[] { new TLStoreChange(TLStoreChange.ChangeType.Remove, id, null) });
            return true;
        }
        return false;
    }

    /// <summary>Get a record by ID, or null.</summary>
    public TLRecord? Get(string id) =>
        _records.TryGetValue(id, out var record) ? record : null;

    /// <summary>Get a typed record by ID.</summary>
    public T? Get<T>(string id) where T : TLRecord =>
        Get(id) as T;

    /// <summary>Check if a record exists.</summary>
    public bool Has(string id) => _records.ContainsKey(id);

    /// <summary>Get all records.</summary>
    public IReadOnlyList<TLRecord> All() => _records.Values.ToList();

    /// <summary>Get all shape records.</summary>
    public IReadOnlyList<TLShapeRecord> GetAllShapes() =>
        _records.Values.OfType<TLShapeRecord>().ToList();

    /// <summary>Get shapes on a specific page (by parent or top-level).</summary>
    public IReadOnlyList<TLShapeRecord> GetPageShapes(string? pageId = null) =>
        _records.Values.OfType<TLShapeRecord>()
            .Where(s => s.ParentId == pageId && !s.IsHidden)
            .OrderBy(s => s.Index)
            .ToList();

    /// <summary>Get all page records.</summary>
    public IReadOnlyList<TLPageRecord> GetPages() =>
        _records.Values.OfType<TLPageRecord>().OrderBy(p => p.Index).ToList();

    /// <summary>Clear all records.</summary>
    public void Clear()
    {
        var ids = _records.Keys.ToList();
        _records.Clear();
        if (ids.Count > 0)
            OnChanged(ids.Select(id => new TLStoreChange(TLStoreChange.ChangeType.Remove, id, null)));
    }

    /// <summary>Total record count.</summary>
    public int Count => _records.Count;


    /// <summary>Create a serializable snapshot of the entire store.</summary>
    public TLStoreSnapshot GetSnapshot() =>
        new() { Records = _records.Values.ToList() };

    /// <summary>Load records from a snapshot, replacing current state.</summary>
    public void LoadSnapshot(TLStoreSnapshot snapshot)
    {
        _records.Clear();
        var changes = new List<TLStoreChange>();
        foreach (var record in snapshot.Records)
        {
            _records[record.Id] = record;
            changes.Add(new TLStoreChange(TLStoreChange.ChangeType.Put, record.Id, record));
        }
        if (changes.Count > 0)
            OnChanged(changes);
    }


    private void OnChanged(IEnumerable<TLStoreChange> changes) =>
        Changed?.Invoke(changes.ToList());
}
