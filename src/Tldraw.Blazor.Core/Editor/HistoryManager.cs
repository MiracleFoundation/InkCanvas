using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Editor;

/// <summary>
/// Manages undo/redo history using store snapshots.
/// Captures a snapshot before each operation and can restore it.
/// </summary>
public class HistoryManager
{
    private readonly Stack<TLStoreSnapshot> _undoStack = new();
    private readonly Stack<TLStoreSnapshot> _redoStack = new();

    /// <summary>Maximum number of undo steps to keep.</summary>
    public int MaxHistory { get; set; } = 50;

    /// <summary>Whether there are actions to undo.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether there are actions to redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Fired when undo/redo availability changes.</summary>
    public event Action? HistoryChanged;

    /// <summary>
    /// Capture the current store state as a snapshot.
    /// Call this BEFORE making changes.
    /// </summary>
    public void PushUndo(TLStore store)
    {
        _undoStack.Push(store.GetSnapshot());
        _redoStack.Clear(); // Clear redo stack on new action

        // Trim history
        while (_undoStack.Count > MaxHistory)
        {
            // Can't easily trim a stack, but we can rebuild
            var temp = _undoStack.ToList();
            _undoStack.Clear();
            foreach (var s in temp.Take(MaxHistory))
                _undoStack.Push(s);
        }

        HistoryChanged?.Invoke();
    }

    /// <summary>Undo the last action by restoring the previous snapshot.</summary>
    public bool Undo(TLStore store)
    {
        if (!CanUndo) return false;

        // Save current state to redo stack
        _redoStack.Push(store.GetSnapshot());

        // Restore previous state
        var snapshot = _undoStack.Pop();
        store.LoadSnapshot(snapshot);

        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>Redo the last undone action.</summary>
    public bool Redo(TLStore store)
    {
        if (!CanRedo) return false;

        // Save current state to undo stack
        _undoStack.Push(store.GetSnapshot());

        // Restore redo state
        var snapshot = _redoStack.Pop();
        store.LoadSnapshot(snapshot);

        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>Clear all history.</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }
}
