using SkiaSharp;
using Tldraw.Blazor.Core.Editor;
using Tldraw.Blazor.Core.Store;
using Editor = Tldraw.Blazor.Core.Editor.Editor;

namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Select tool — the default tool for selecting, moving, and managing shapes.
/// Child states: Idle → Pointing → Dragging | Brush | Resizing
/// </summary>
public class SelectTool : StateNode
{
    public override string Id => "select";

    public SelectTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new PointingState(this));
        RegisterChild(new DraggingState(this));
        RegisterChild(new BrushState(this));
        RegisterChild(new ResizingState(this));
        RegisterChild(new RotatingState(this));
        RegisterChild(new ArrowEditingState(this));
    }

    public override void OnEnter()
    {
        Transition("idle");
    }

    // ── Idle State ──────────────────────────────────────────

    public class IdleState : StateNode
    {
        private readonly SelectTool _tool;
        public override string Id => "idle";

        public IdleState(SelectTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            // 1. Check if clicking an arrow endpoint
            var arrowEndpoint = Editor.HitTestArrowEndpoint(e.WorldX, e.WorldY);
            if (arrowEndpoint.HasValue)
            {
                var (arrowId, endpoint) = arrowEndpoint.Value;
                Editor.Selection.ClearSelection();
                Editor.Selection.Select(arrowId);
                _tool.EditingArrowId = arrowId;
                _tool.EditingArrowEndpoint = endpoint;
                Parent?.Transition("arrowEditing");
                return;
            }

            // 2. Check if clicking a resize/rotation handle
            if (Editor.Selection.Count > 0)
            {
                var handle = Editor.Selection.HitTestHandles(
                    new SKPointd(e.WorldX, e.WorldY),
                    (float)Editor.Camera.Zoom,
                    Editor.ShapeUtils);

                if (handle == SelectionManager.HandleType.Rotation)
                {
                    _tool.RotationStartWorld = new SKPointd(e.WorldX, e.WorldY);
                    _tool.SaveRotationStart(Editor);
                    Parent?.Transition("rotating");
                    return;
                }

                if (handle != SelectionManager.HandleType.None)
                {
                    _tool.ActiveHandle = handle;
                    _tool.ResizeStartWorld = new SKPointd(e.WorldX, e.WorldY);
                    _tool.SaveShapeBounds(Editor);
                    Parent?.Transition("resizing");
                    return;
                }
            }

            // 2. Check if clicking a shape
            var hitShape = FindShapeAtPoint(e.WorldX, e.WorldY);

            if (hitShape != null)
            {
                if (e.ShiftKey)
                {
                    Editor.Selection.Toggle(hitShape.Id);
                }
                else if (!Editor.Selection.IsSelected(hitShape.Id))
                {
                    Editor.Selection.ClearSelection();
                    Editor.Selection.Select(hitShape.Id);
                }

                _tool.PointedShapeId = hitShape.Id;
                _tool.PointerStartWorld = new SKPointd(e.WorldX, e.WorldY);
                _tool.PointerStartScreen = new SKPoint((float)e.ScreenX, (float)e.ScreenY);
                Parent?.Transition("pointing");
            }
            else
            {
                // 3. Click on empty space → brush selection
                if (!e.ShiftKey)
                    Editor.Selection.ClearSelection();

                _tool.PointerStartWorld = new SKPointd(e.WorldX, e.WorldY);
                _tool.PointerStartScreen = new SKPoint((float)e.ScreenX, (float)e.ScreenY);
                Parent?.Transition("brush");
            }

            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Delete" || e.Key == "Backspace")
            {
                DeleteSelectedShapes();
            }
            else if (e.Key == "a" && e.CtrlKey)
            {
                Editor.Selection.SelectAll(
                    Editor.Store.GetAllShapes().Select(s => s.Id));
                Editor.Invalidate();
            }
            else if (e.Key == "Escape")
            {
                Editor.Selection.ClearSelection();
                Editor.Invalidate();
            }
        }

        private void DeleteSelectedShapes()
        {
            var ids = Editor.Selection.SelectedIds.ToList();
            Editor.PushUndo();
            Editor.Selection.ClearSelection();
            foreach (var id in ids)
                Editor.Store.Remove(id);
            Editor.Invalidate();
        }

        private TLShapeRecord? FindShapeAtPoint(double worldX, double worldY)
        {
            var shapes = Editor.Store.GetPageShapes();
            var point = new SKPointd(worldX, worldY);

            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                var shape = shapes[i];
                if (shape.IsLocked || shape.IsHidden) continue;

                var util = Editor.ShapeUtils.Get(shape.ShapeType);
                if (util != null && util.HitTest(shape, point))
                    return shape;
            }
            return null;
        }
    }

    // ── Pointing State ──────────────────────────────────────

    public class PointingState : StateNode
    {
        private readonly SelectTool _tool;
        private const double DragThreshold = 5.0;

        public override string Id => "pointing";

        public PointingState(SelectTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            var dx = e.ScreenX - _tool.PointerStartScreen.X;
            var dy = e.ScreenY - _tool.PointerStartScreen.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist > DragThreshold)
            {
                _tool.DragStartWorld = new SKPointd(e.WorldX, e.WorldY);
                Parent?.Transition("dragging");
            }
        }

        public override void OnPointerUp(PointerEvent e)
        {
            Parent?.Transition("idle");
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                Editor.Selection.ClearSelection();
                Editor.Invalidate();
                Parent?.Transition("idle");
            }
        }
    }

    // ── Dragging State (move shapes) ────────────────────────

    public class DraggingState : StateNode
    {
        private readonly SelectTool _tool;
        private SKPointd _lastWorld;

        public override string Id => "dragging";

        public DraggingState(SelectTool tool) => _tool = tool;

        public override void OnEnter()
        {
            _lastWorld = _tool.DragStartWorld;
            Editor.PushUndo();
        }

        public override void OnPointerMove(PointerEvent e)
        {
            var currentWorld = new SKPointd(e.WorldX, e.WorldY);
            var dx = currentWorld.X - _lastWorld.X;
            var dy = currentWorld.Y - _lastWorld.Y;

            if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01) return;

            var selectedShapes = Editor.Selection.GetSelectedShapes(Editor.Store);
            foreach (var shape in selectedShapes)
            {
                if (shape.IsLocked) continue;
                shape.X += dx;
                shape.Y += dy;
            }

            // Update arrow bindings after moving shapes
            Editor.UpdateAllArrowBindings();

            _lastWorld = currentWorld;
            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            Parent?.Transition("idle");
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
                Parent?.Transition("idle");
        }
    }

    // ── Brush (Rubber-band) State ───────────────────────────

    public class BrushState : StateNode
    {
        private readonly SelectTool _tool;
        private SKPointd _brushStart;
        private SKPointd _brushEnd;

        public override string Id => "brush";

        public BrushState(SelectTool tool) => _tool = tool;

        public override void OnEnter()
        {
            _brushStart = _tool.PointerStartWorld;
            _brushEnd = _brushStart;
        }

        public override void OnPointerMove(PointerEvent e)
        {
            _brushEnd = new SKPointd(e.WorldX, e.WorldY);
            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            _brushEnd = new SKPointd(e.WorldX, e.WorldY);
            var brushRect = MakeBrushRect();
            var shapes = Editor.Store.GetPageShapes();

            foreach (var shape in shapes)
            {
                if (shape.IsLocked || shape.IsHidden) continue;
                var util = Editor.ShapeUtils.Get(shape.ShapeType);
                if (util == null) continue;
                if (RectsIntersect(brushRect, util.GetBounds(shape)))
                    Editor.Selection.Select(shape.Id);
            }

            Editor.Invalidate();
            Parent?.Transition("idle");
        }

        public override void Render(SKCanvas canvas, float zoom)
        {
            var rect = MakeBrushRect();
            using var fillPaint = new SKPaint
            {
                Color = new SKColor(0x00, 0x78, 0xD4, 30),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            using var strokePaint = new SKPaint
            {
                Color = new SKColor(0x00, 0x78, 0xD4, 120),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f / zoom,
                IsAntialias = true,
            };
            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, strokePaint);
        }

        private SKRect MakeBrushRect()
        {
            float left = (float)Math.Min(_brushStart.X, _brushEnd.X);
            float top = (float)Math.Min(_brushStart.Y, _brushEnd.Y);
            float right = (float)Math.Max(_brushStart.X, _brushEnd.X);
            float bottom = (float)Math.Max(_brushStart.Y, _brushEnd.Y);
            return new SKRect(left, top, right, bottom);
        }

        private static bool RectsIntersect(SKRect a, SKRect b) =>
            a.Left < b.Right && a.Right > b.Left &&
            a.Top < b.Bottom && a.Bottom > b.Top;
    }

    // ── Resizing State ──────────────────────────────────────

    public class ResizingState : StateNode
    {
        private readonly SelectTool _tool;
        public override string Id => "resizing";

        // Snapshot of shape bounds at resize start
        private Dictionary<string, (double X, double Y, double W, double H)> _initialBounds = new();

        public ResizingState(SelectTool tool) => _tool = tool;

        public override void OnEnter()
        {
            _initialBounds = new Dictionary<string, (double, double, double, double)>(_tool.SavedShapeBounds);
            Editor.PushUndo();
        }

        public override void OnPointerMove(PointerEvent e)
        {
            var handle = _tool.ActiveHandle;
            var startWorld = _tool.ResizeStartWorld;
            var currentWorld = new SKPointd(e.WorldX, e.WorldY);

            var dx = currentWorld.X - startWorld.X;
            var dy = currentWorld.Y - startWorld.Y;

            var selectedShapes = Editor.Selection.GetSelectedShapes(Editor.Store);

            foreach (var shape in selectedShapes)
            {
                if (shape.IsLocked) continue;
                if (!_initialBounds.TryGetValue(shape.Id, out var initial)) continue;

                ApplyResize(shape, initial, handle, dx, dy, e.ShiftKey, e.AltKey);
            }

            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            Parent?.Transition("idle");
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                // Restore initial bounds (cancel resize)
                foreach (var (id, bounds) in _initialBounds)
                {
                    var shape = Editor.Store.Get<TLShapeRecord>(id);
                    if (shape == null) continue;
                    shape.X = bounds.X;
                    shape.Y = bounds.Y;
                    shape.Width = bounds.W;
                    shape.Height = bounds.H;
                }
                Editor.Invalidate();
                Parent?.Transition("idle");
            }
        }

        private void ApplyResize(TLShapeRecord shape, (double X, double Y, double W, double H) initial,
            SelectionManager.HandleType handle, double dx, double dy, bool shiftKey, bool altKey)
        {
            double newX = initial.X;
            double newY = initial.Y;
            double newW = initial.W;
            double newH = initial.H;

            // Compute new bounds based on which handle is being dragged
            switch (handle)
            {
                case SelectionManager.HandleType.BottomRight:
                    newW = initial.W + dx;
                    newH = initial.H + dy;
                    if (altKey) { newX = initial.X - dx / 2; newY = initial.Y - dy / 2; newW = initial.W + dx; newH = initial.H + dy; }
                    break;

                case SelectionManager.HandleType.TopLeft:
                    newX = initial.X + dx;
                    newY = initial.Y + dy;
                    newW = initial.W - dx;
                    newH = initial.H - dy;
                    if (altKey) { newX = initial.X + dx / 2; newY = initial.Y + dy / 2; newW = initial.W - dx; newH = initial.H - dy; }
                    break;

                case SelectionManager.HandleType.TopRight:
                    newY = initial.Y + dy;
                    newW = initial.W + dx;
                    newH = initial.H - dy;
                    if (altKey) { newY = initial.Y + dy / 2; newW = initial.W + dx; newH = initial.H - dy; }
                    break;

                case SelectionManager.HandleType.BottomLeft:
                    newX = initial.X + dx;
                    newW = initial.W - dx;
                    newH = initial.H + dy;
                    if (altKey) { newX = initial.X + dx / 2; newW = initial.W - dx; newH = initial.H + dy; }
                    break;

                case SelectionManager.HandleType.TopCenter:
                    newY = initial.Y + dy;
                    newH = initial.H - dy;
                    if (altKey) { newY = initial.Y + dy / 2; newH = initial.H - dy; }
                    break;

                case SelectionManager.HandleType.BottomCenter:
                    newH = initial.H + dy;
                    if (altKey) { newH = initial.H + dy; }
                    break;

                case SelectionManager.HandleType.MidLeft:
                    newX = initial.X + dx;
                    newW = initial.W - dx;
                    if (altKey) { newX = initial.X + dx / 2; newW = initial.W - dx; }
                    break;

                case SelectionManager.HandleType.MidRight:
                    newW = initial.W + dx;
                    if (altKey) { newW = initial.W + dx; }
                    break;
            }

            // Enforce minimum size
            const double minSize = 5;
            if (newW < minSize)
            {
                if (handle is SelectionManager.HandleType.TopLeft or
                    SelectionManager.HandleType.BottomLeft or
                    SelectionManager.HandleType.MidLeft)
                {
                    newX = initial.X + initial.W - minSize;
                }
                newW = minSize;
            }
            if (newH < minSize)
            {
                if (handle is SelectionManager.HandleType.TopLeft or
                    SelectionManager.HandleType.TopCenter or
                    SelectionManager.HandleType.TopRight)
                {
                    newY = initial.Y + initial.H - minSize;
                }
                newH = minSize;
            }

            // Shift = constrain proportions
            if (shiftKey && initial.W > 0 && initial.H > 0)
            {
                var aspect = initial.W / initial.H;
                if (handle is SelectionManager.HandleType.TopLeft or
                    SelectionManager.HandleType.TopRight or
                    SelectionManager.HandleType.BottomLeft or
                    SelectionManager.HandleType.BottomRight)
                {
                    // Use the larger dimension to drive both
                    if (Math.Abs(dx) > Math.Abs(dy))
                        newH = newW / aspect;
                    else
                        newW = newH * aspect;
                }
            }

            shape.X = newX;
            shape.Y = newY;
            shape.Width = newW;
            shape.Height = newH;
        }
    }

    // ── Rotating State ──────────────────────────────────────

    public class RotatingState : StateNode
    {
        private readonly SelectTool _tool;
        public override string Id => "rotating";

        private Dictionary<string, double> _initialRotations = new();
        private SKPointd _center;

        public RotatingState(SelectTool tool) => _tool = tool;

        public override void OnEnter()
        {
            Editor.PushUndo();
            _initialRotations = new Dictionary<string, double>(_tool.SavedRotations);

            // Compute center of selection
            var bounds = Editor.Selection.GetCombinedBounds(Editor.ShapeUtils);
            if (bounds.HasValue)
            {
                _center = new SKPointd(bounds.Value.MidX, bounds.Value.MidY);
            }
        }

        public override void OnPointerMove(PointerEvent e)
        {
            var currentWorld = new SKPointd(e.WorldX, e.WorldY);

            // Calculate angle from center to current pointer
            var startAngle = Math.Atan2(
                _tool.RotationStartWorld.Y - _center.Y,
                _tool.RotationStartWorld.X - _center.X);
            var currentAngle = Math.Atan2(
                currentWorld.Y - _center.Y,
                currentWorld.X - _center.X);
            var deltaAngle = currentAngle - startAngle;

            // Apply rotation to all selected shapes
            foreach (var (id, initialRot) in _initialRotations)
            {
                var shape = Editor.Store.Get<TLShapeRecord>(id);
                if (shape == null || shape.IsLocked) continue;

                shape.Rotation = initialRot + deltaAngle;

                // Snap to 15-degree increments if Shift is held
                if (e.ShiftKey)
                {
                    var snapAngle = Math.PI / 12; // 15 degrees
                    shape.Rotation = Math.Round(shape.Rotation / snapAngle) * snapAngle;
                }
            }

            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            Parent?.Transition("idle");
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                // Restore initial rotations
                foreach (var (id, initialRot) in _initialRotations)
                {
                    var shape = Editor.Store.Get<TLShapeRecord>(id);
                    if (shape != null)
                        shape.Rotation = initialRot;
                }
                Editor.Invalidate();
                Parent?.Transition("idle");
            }
        }
    }

    // ── Arrow Editing State ─────────────────────────────────

    public class ArrowEditingState : StateNode
    {
        private readonly SelectTool _tool;
        public override string Id => "arrowEditing";

        private string? _arrowId;
        private string? _endpoint; // "start" or "end"
        private bool _isBound;

        public ArrowEditingState(SelectTool tool) => _tool = tool;

        public override void OnEnter()
        {
            _arrowId = _tool.EditingArrowId;
            _endpoint = _tool.EditingArrowEndpoint;
            _isBound = _arrowId != null && Editor.GetArrowBinding(_arrowId, _endpoint!) != null;

            Editor.PushUndo();
        }

        public override void OnPointerMove(PointerEvent e)
        {
            if (_arrowId == null || _endpoint == null) return;

            var arrow = Editor.Store.Get<TLShapeRecord>(_arrowId);
            if (arrow?.Props is not TLArrowProps arrowProps) return;

            // Move the endpoint to follow the pointer
            if (_endpoint == "start")
            {
                // Move arrow origin
                var dx = e.WorldX - arrow.X;
                var dy = e.WorldY - arrow.Y;
                arrow.X = e.WorldX;
                arrow.Y = e.WorldY;

                // Adjust all waypoints
                for (int i = 0; i < arrowProps.Waypoints.Count; i++)
                {
                    arrowProps.Waypoints[i][0] -= dx;
                    arrowProps.Waypoints[i][1] -= dy;
                }
            }
            else // "end"
            {
                // Move last waypoint
                if (arrowProps.Waypoints.Count >= 2)
                {
                    arrowProps.Waypoints[^1][0] = e.WorldX - arrow.X;
                    arrowProps.Waypoints[^1][1] = e.WorldY - arrow.Y;
                }
            }

            // Try to bind to nearby shape
            var binding = Editor.TryBindArrowEndpoint(_arrowId, _endpoint, e.WorldX, e.WorldY);
            _isBound = binding != null;

            // Remove old binding if endpoint moved away
            if (binding == null)
                Editor.UnbindArrowEndpoint(_arrowId, _endpoint);

            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            if (_arrowId != null && _endpoint != null)
            {
                // Final binding attempt
                Editor.TryBindArrowEndpoint(_arrowId, _endpoint, e.WorldX, e.WorldY);
            }

            _arrowId = null;
            _endpoint = null;
            Parent?.Transition("idle");
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                // Cancel editing — restore would be complex, just commit
                _arrowId = null;
                _endpoint = null;
                Parent?.Transition("idle");
                Editor.Invalidate();
            }
        }

        public override void Render(SKCanvas canvas, float zoom)
        {
            if (_arrowId == null || _endpoint == null) return;

            // Draw binding indicator if near a shape
            if (_isBound)
            {
                using var bindPaint = new SKPaint
                {
                    Color = new SKColor(0x22, 0xC5, 0x5E, 180), // green
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                };
                float radius = 8f / zoom;
                canvas.DrawCircle((float)Editor.PointerWorld.X, (float)Editor.PointerWorld.Y, radius, bindPaint);
            }
        }
    }

    // ── Shared state ────────────────────────────────────────

    internal string? PointedShapeId;
    internal SKPointd PointerStartWorld;
    internal SKPoint PointerStartScreen;
    internal SKPointd DragStartWorld;

    // Resize state
    internal SelectionManager.HandleType ActiveHandle;
    internal SKPointd ResizeStartWorld;
    internal Dictionary<string, (double X, double Y, double W, double H)> SavedShapeBounds = new();

    // Rotation state
    internal SKPointd RotationStartWorld;
    internal Dictionary<string, double> SavedRotations = new();

    // Arrow editing state
    internal string? EditingArrowId;
    internal string? EditingArrowEndpoint;

    /// <summary>Save current bounds of all selected shapes for resize calculations.</summary>
    internal void SaveShapeBounds(Tldraw.Blazor.Core.Editor.Editor editor)
    {
        SavedShapeBounds.Clear();
        var shapes = editor.Selection.GetSelectedShapes(editor.Store);
        foreach (var shape in shapes)
            SavedShapeBounds[shape.Id] = (shape.X, shape.Y, shape.Width, shape.Height);
    }

    /// <summary>Save current rotations for rotation calculations.</summary>
    internal void SaveRotationStart(Tldraw.Blazor.Core.Editor.Editor editor)
    {
        SavedRotations.Clear();
        var shapes = editor.Selection.GetSelectedShapes(editor.Store);
        foreach (var shape in shapes)
            SavedRotations[shape.Id] = shape.Rotation;
    }
}
