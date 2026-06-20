using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using InkCanvas.Core;
using InkCanvas.Core.Editor;
using static InkCanvas.Core.Editor.SelectionManager;

namespace InkCanvas.Canvas;

public partial class TldrawCanvas : IDisposable
{
    private SKCanvasView? _canvasView;
    private Core.Editor.Editor _editor = new();
    private bool _disposed;

    /// <summary>The editor instance. Can be set externally or defaults to a new instance.</summary>
    [Parameter]
    public Core.Editor.Editor? Editor { get; set; }

    private readonly Dictionary<long, SKPoint> _activePointers = new();
    private double _pinchStartDistance;
    private SKPoint _pinchStartCenter;
    private bool _isPinching;

    /// <summary>Cursor style based on current tool/state.</summary>
    private string CursorStyle
    {
        get
        {
            if (_editor.IsSpaceHeld)
                return _editor.IsPointerDown ? "grabbing" : "grab";

            var toolId = _editor.ActiveTool.Id;
            return toolId switch
            {
                StateId.Hand => _editor.IsPointerDown ? "grabbing" : "grab",
                StateId.Draw => "crosshair",
                StateId.Geo => "crosshair",
                StateId.Arrow => "crosshair",
                StateId.Eraser => "crosshair",
                StateId.Text => "text",
                StateId.Select => GetSelectCursor(),
                _ => "default"
            };
        }
    }

    private string GetSelectCursor()
    {
        if (_editor.IsPointerDown) return "grabbing";

        return _editor.HoveredHandle switch
        {
            SelectionManager.HandleType.TopLeft => "nwse-resize",
            SelectionManager.HandleType.BottomRight => "nwse-resize",
            SelectionManager.HandleType.TopRight => "nesw-resize",
            SelectionManager.HandleType.BottomLeft => "nesw-resize",
            SelectionManager.HandleType.TopCenter => "ns-resize",
            SelectionManager.HandleType.BottomCenter => "ns-resize",
            SelectionManager.HandleType.MidLeft => "ew-resize",
            SelectionManager.HandleType.MidRight => "ew-resize",
            SelectionManager.HandleType.Rotation => "crosshair",
            _ => "default"
        };
    }

    protected override void OnInitialized()
    {
        if (Editor is not null)
            _editor = Editor;

        _editor.StateChanged += OnEditorStateChanged;
    }

    private void OnEditorStateChanged()
    {
        InvokeAsync(() =>
        {
            _canvasView?.Invalidate();
            StateHasChanged();
        });
    }

    private void OnPaintSurface(SKPaintSurfaceEventArgs args)
    {
        _editor.Render(args.Surface.Canvas, args.Info);
    }


    private void OnPointerDown(PointerEventArgs e)
    {
        _activePointers[e.PointerId] = new SKPoint((float)e.OffsetX, (float)e.OffsetY);

        if (_activePointers.Count == 2)
        {
            // Two fingers down → start pinch
            StartPinch();
            return;
        }

        if (_activePointers.Count == 1)
            _editor.OnPointerDown(e.OffsetX, e.OffsetY, (int)e.PointerId,
                e.ShiftKey, e.AltKey, e.CtrlKey);
    }

    private void OnDoubleClick(MouseEventArgs e)
    {
        _editor.OnDoubleClick(e.OffsetX, e.OffsetY);
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        _activePointers[e.PointerId] = new SKPoint((float)e.OffsetX, (float)e.OffsetY);

        if (_isPinching && _activePointers.Count == 2)
        {
            UpdatePinch();
            return;
        }

        if (_activePointers.Count == 1)
            _editor.OnPointerMove(e.OffsetX, e.OffsetY, (int)e.PointerId,
                e.ShiftKey, e.AltKey, e.CtrlKey);
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        _activePointers.Remove(e.PointerId);

        if (_isPinching && _activePointers.Count < 2)
        {
            EndPinch();
            return;
        }

        if (_activePointers.Count == 0)
            _editor.OnPointerUp(e.OffsetX, e.OffsetY, (int)e.PointerId,
                e.ShiftKey, e.AltKey, e.CtrlKey);
    }


    private void StartPinch()
    {
        var points = _activePointers.Values.ToArray();
        _pinchStartDistance = Distance(points[0], points[1]);
        _pinchStartCenter = MidPoint(points[0], points[1]);
        _isPinching = true;
    }

    private void UpdatePinch()
    {
        var points = _activePointers.Values.ToArray();
        var currentDistance = Distance(points[0], points[1]);
        var currentCenter = MidPoint(points[0], points[1]);

        if (_pinchStartDistance <= 0) return;

        // Zoom
        var zoomDelta = (currentDistance - _pinchStartDistance) / _pinchStartDistance;
        _editor.Camera.ZoomAt(new SKPoint(_pinchStartCenter.X, _pinchStartCenter.Y), zoomDelta * 0.5);

        // Pan
        var panDx = currentCenter.X - _pinchStartCenter.X;
        var panDy = currentCenter.Y - _pinchStartCenter.Y;
        _editor.Camera.Pan(panDx, panDy);

        _pinchStartDistance = currentDistance;
        _pinchStartCenter = currentCenter;
        _editor.Invalidate();
    }

    private void EndPinch()
    {
        _isPinching = false;
        _activePointers.Clear();
    }

    private static double Distance(SKPoint a, SKPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static SKPoint MidPoint(SKPoint a, SKPoint b)
    {
        return new SKPoint((a.X + b.X) / 2, (a.Y + b.Y) / 2);
    }


    private void OnKeyDown(KeyboardEventArgs e)
    {
        var key = e.Key;
        if (key == "Del") key = "Delete";
        _editor.OnKeyDown(key, e.ShiftKey, e.AltKey, e.CtrlKey);
    }

    private void OnKeyUp(KeyboardEventArgs e)
    {
        _editor.OnKeyUp(e.Key);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _editor.StateChanged -= OnEditorStateChanged;
            _disposed = true;
        }
    }
}
