using SkiaSharp;
using Tldraw.Blazor.Core.Editor;
using Tldraw.Blazor.Core.Store;
using Editor = Tldraw.Blazor.Core.Editor.Editor;

namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Draw tool — creates freehand strokes by dragging.
/// Child states: Idle → Drawing
/// </summary>
public class DrawTool : StateNode
{
    public override string Id => "draw";

    public DrawTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new DrawingState(this));
    }

    public override void OnEnter()
    {
        Transition("idle");
    }

    // ── Idle State ──────────────────────────────────────────

    public class IdleState : StateNode
    {
        private readonly DrawTool _tool;
        public override string Id => "idle";

        public IdleState(DrawTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            // Create a new draw shape at the pointer position
            var shape = new TLShapeRecord
            {
                ShapeType = "draw",
                X = e.WorldX,
                Y = e.WorldY,
                Width = 0,
                Height = 0,
                Style = new TLShapeStyle
                {
                    Color = "#1e1e1e",
                    Fill = "none",
                    StrokeWidth = 2.5,
                },
                Props = new TLDrawProps
                {
                    Segments = new List<List<double>>
                    {
                        new() { 0, 0 } // first point relative to shape origin
                    }
                }
            };

            Editor.Store.Put(shape);
            _tool.CurrentShapeId = shape.Id;
            _tool.LastWorld = new SKPointd(e.WorldX, e.WorldY);
            Parent?.Transition("drawing");
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                // Switch back to select tool
                Editor.SetActiveTool("select");
            }
        }
    }

    // ── Drawing State ───────────────────────────────────────

    public class DrawingState : StateNode
    {
        private readonly DrawTool _tool;
        public override string Id => "drawing";

        public DrawingState(DrawTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            if (_tool.CurrentShapeId == null) return;

            var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);
            if (shape?.Props is not TLDrawProps draw) return;

            // Add point relative to shape origin
            var relX = e.WorldX - shape.X;
            var relY = e.WorldY - shape.Y;

            var lastSegment = draw.Segments.LastOrDefault();
            if (lastSegment != null)
            {
                lastSegment.Add(relX);
                lastSegment.Add(relY);
            }

            // Update bounding box
            shape.Width = Math.Max(shape.Width, Math.Abs(relX) + 10);
            shape.Height = Math.Max(shape.Height, Math.Abs(relY) + 10);

            _tool.LastWorld = new SKPointd(e.WorldX, e.WorldY);
            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            if (_tool.CurrentShapeId != null)
            {
                var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);
                if (shape?.Props is TLDrawProps draw)
                {
                    draw.IsComplete = true;
                }
            }

            _tool.CurrentShapeId = null;
            Parent?.Transition("idle");
            Editor.Invalidate();
        }
    }

    // ── Shared state ────────────────────────────────────────

    internal string? CurrentShapeId;
    internal SKPointd LastWorld;
}
