using SkiaSharp;

namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Abstract base for tool states. Implements a hierarchical state machine.
/// Each state can have child states. Only one child is active at a time.
/// </summary>
public abstract class StateNode
{
    /// <summary>State identifier.</summary>
    public abstract string Id { get; }

    /// <summary>Reference to the editor.</summary>
    public Tldraw.Blazor.Core.Editor.Editor Editor { get; internal set; } = null!;

    /// <summary>Parent state (null for root tool).</summary>
    public StateNode? Parent { get; internal set; }

    /// <summary>Child states by ID.</summary>
    public Dictionary<string, StateNode> Children { get; } = new();

    /// <summary>Currently active child state.</summary>
    public StateNode? ActiveChild { get; private set; }

    /// <summary>The root tool (topmost parent).</summary>
    public StateNode Root => Parent?.Root ?? this;


    /// <summary>Called when this state becomes active.</summary>
    public virtual void OnEnter() { }

    /// <summary>Called when this state is exited.</summary>
    public virtual void OnExit() { }


    public virtual void OnPointerDown(PointerEvent e) =>
        ActiveChild?.OnPointerDown(e);

    public virtual void OnPointerMove(PointerEvent e) =>
        ActiveChild?.OnPointerMove(e);

    public virtual void OnPointerUp(PointerEvent e) =>
        ActiveChild?.OnPointerUp(e);

    public virtual void OnKeyDown(KeyEvent e) =>
        ActiveChild?.OnKeyDown(e);


    /// <summary>Render overlays for this state (selection, brush rect, etc.).</summary>
    public virtual void Render(SKCanvas canvas, float zoom) =>
        ActiveChild?.Render(canvas, zoom);


    /// <summary>Register a child state.</summary>
    protected void RegisterChild(StateNode child)
    {
        child.Editor = Editor;
        child.Parent = this;
        Children[child.Id] = child;
    }

    /// <summary>Transition to a child state by ID.</summary>
    public void Transition(string childId)
    {
        if (!Children.TryGetValue(childId, out var next))
            throw new InvalidOperationException($"Child state '{childId}' not found in '{Id}'");

        Transition(next);
    }

    /// <summary>Transition to a child state.</summary>
    public void Transition(StateNode next)
    {
        if (ActiveChild == next) return;

        ActiveChild?.OnExit();
        ActiveChild = next;
        ActiveChild.Editor = Editor;
        ActiveChild.OnEnter();
    }

    /// <summary>Return to parent state (or stay if root).</summary>
    public void ReturnToParent()
    {
        Parent?.Transition((string?)null!);
        Parent?.OnReturnedFromChild(this);
    }

    /// <summary>Called when a child state returns to this parent.</summary>
    protected virtual void OnReturnedFromChild(StateNode child) { }
}
