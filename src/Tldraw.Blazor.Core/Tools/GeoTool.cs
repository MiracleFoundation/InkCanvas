using SkiaSharp;
using Tldraw.Blazor.Core.Editor;
using Tldraw.Blazor.Core.Store;
using Editor = Tldraw.Blazor.Core.Editor.Editor;

namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Geo tool — creates geometric shapes by dragging to define bounds.
/// Child states: Idle → Creating
/// </summary>
public class GeoTool : StateNode
{
    public override string Id => "geo";

    /// <summary>The geo type to create (rectangle, ellipse, diamond, star, hexagon, triangle).</summary>
    public string GeoType { get; set; } = "rectangle";

    public GeoTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new CreatingState(this));
    }

    public override void OnEnter()
    {
        Transition("idle");
    }

    // ── Idle State ──────────────────────────────────────────

    public class IdleState : StateNode
    {
        private readonly GeoTool _tool;
        public override string Id => "idle";

        public IdleState(GeoTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            _tool.DragStart = new SKPointd(e.WorldX, e.WorldY);
            _tool.DragEnd = _tool.DragStart;

            // Create shape with zero size at start point
            var shape = new TLShapeRecord
            {
                ShapeType = "geo",
                X = e.WorldX,
                Y = e.WorldY,
                Width = 0,
                Height = 0,
                Style = new TLShapeStyle
                {
                    Color = "#1e1e1e",
                    Fill = "#E3F2FD",
                    StrokeWidth = 2,
                },
                Props = new TLGeoProps
                {
                    GeoType = _tool.GeoType,
                }
            };

            Editor.Store.Put(shape);
            _tool.CurrentShapeId = shape.Id;
            Parent?.Transition("creating");
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                Editor.SetActiveTool("select");
            }
        }
    }

    // ── Creating State ──────────────────────────────────────

    public class CreatingState : StateNode
    {
        private readonly GeoTool _tool;
        public override string Id => "creating";

        public CreatingState(GeoTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            if (_tool.CurrentShapeId == null) return;

            var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);
            if (shape == null) return;

            _tool.DragEnd = new SKPointd(e.WorldX, e.WorldY);

            // Compute bounding rect from start to current
            double left = Math.Min(_tool.DragStart.X, e.WorldX);
            double top = Math.Min(_tool.DragStart.Y, e.WorldY);
            double right = Math.Max(_tool.DragStart.X, e.WorldX);
            double bottom = Math.Max(_tool.DragStart.Y, e.WorldY);

            shape.X = left;
            shape.Y = top;
            shape.Width = right - left;
            shape.Height = bottom - top;

            // Hold Shift to constrain to square/circle
            if (e.ShiftKey)
            {
                var size = Math.Max(shape.Width, shape.Height);
                shape.Width = size;
                shape.Height = size;
            }

            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            if (_tool.CurrentShapeId != null)
            {
                var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);

                // If shape is too small (just a click), give it a default size
                if (shape != null && shape.Width < 5 && shape.Height < 5)
                {
                    shape.Width = 160;
                    shape.Height = 100;
                }
            }

            _tool.CurrentShapeId = null;
            Parent?.Transition("idle");
            Editor.Invalidate();
        }
    }

    // ── Shared state ────────────────────────────────────────

    internal string? CurrentShapeId;
    internal SKPointd DragStart;
    internal SKPointd DragEnd;
}
