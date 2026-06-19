namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Unified pointer event data passed to tool states.
/// </summary>
public record PointerEvent
{
    public double ScreenX { get; init; }
    public double ScreenY { get; init; }
    public double WorldX { get; init; }
    public double WorldY { get; init; }
    public int PointerId { get; init; }
    public bool ShiftKey { get; init; }
    public bool AltKey { get; init; }
    public bool CtrlKey { get; init; }
}

/// <summary>
/// Keyboard event data.
/// </summary>
public record KeyEvent
{
    public string Key { get; init; } = "";
    public bool ShiftKey { get; init; }
    public bool AltKey { get; init; }
    public bool CtrlKey { get; init; }
}
