using SkiaSharp;
using Tldraw.Blazor.Core.Editor;
using Tldraw.Blazor.Core.Store;
using Editor = Tldraw.Blazor.Core.Editor.Editor;

namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Arrow tool — drag to create an arrow shape.
/// Child states: Idle → Drawing
/// </summary>
public class ArrowTool : StateNode
{
    public override string Id => "arrow";

    public ArrowTool()
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
        private readonly ArrowTool _tool;
        public override string Id => "idle";

        public IdleState(ArrowTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            _tool.StartPoint = new SKPointd(e.WorldX, e.WorldY);
            _tool.EndPoint = _tool.StartPoint;

            var shape = new TLShapeRecord
            {
                ShapeType = "arrow",
                X = e.WorldX,
                Y = e.WorldY,
                Width = 0,
                Height = 0,
                Style = new TLShapeStyle
                {
                    Color = "#1e1e1e",
                    Fill = "none",
                    StrokeWidth = 2,
                },
                Props = new TLArrowProps
                {
                    Waypoints = new List<List<double>>
                    {
                        new() { 0, 0 },
                        new() { 0, 0 }
                    }
                }
            };

            Editor.Store.Put(shape);
            _tool.CurrentShapeId = shape.Id;
            Parent?.Transition("drawing");
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
                Editor.SetActiveTool("select");
        }
    }

    // ── Drawing State ───────────────────────────────────────

    public class DrawingState : StateNode
    {
        private readonly ArrowTool _tool;
        public override string Id => "drawing";

        public DrawingState(ArrowTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            if (_tool.CurrentShapeId == null) return;

            var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);
            if (shape?.Props is not TLArrowProps arrow) return;

            _tool.EndPoint = new SKPointd(e.WorldX, e.WorldY);

            // Update waypoints (relative to shape origin)
            arrow.Waypoints[1] = new List<double>
            {
                e.WorldX - shape.X,
                e.WorldY - shape.Y
            };

            // Update bounding box
            var minX = Math.Min(0, e.WorldX - shape.X);
            var minY = Math.Min(0, e.WorldY - shape.Y);
            var maxX = Math.Max(0, e.WorldX - shape.X);
            var maxY = Math.Max(0, e.WorldY - shape.Y);

            // Adjust shape position if end point extends before start
            if (e.WorldX < shape.X)
            {
                shape.Width = shape.X + shape.Width - e.WorldX;
                shape.X = e.WorldX;
                arrow.Waypoints[0][0] = _tool.StartPoint.X - shape.X;
                arrow.Waypoints[1][0] = 0;
            }
            else
            {
                shape.Width = e.WorldX - shape.X;
            }

            if (e.WorldY < shape.Y)
            {
                shape.Height = shape.Y + shape.Height - e.WorldY;
                shape.Y = e.WorldY;
                arrow.Waypoints[0][1] = _tool.StartPoint.Y - shape.Y;
                arrow.Waypoints[1][1] = 0;
            }
            else
            {
                shape.Height = e.WorldY - shape.Y;
            }

            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            if (_tool.CurrentShapeId != null)
            {
                var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);
                if (shape != null)
                {
                    // If too small (just a click), give default size
                    if (Math.Abs(shape.Width) < 5 && Math.Abs(shape.Height) < 5)
                    {
                        shape.Width = 200;
                        shape.Height = 0;
                        if (shape.Props is TLArrowProps arrow)
                        {
                            arrow.Waypoints[1] = new List<double> { 200, 0 };
                        }
                    }

                    // Try to bind endpoints to nearby shapes
                    if (shape.Props is TLArrowProps arrowProps && arrowProps.Waypoints.Count >= 2)
                    {
                        // Bind start
                        var startX = shape.X + arrowProps.Waypoints[0][0];
                        var startY = shape.Y + arrowProps.Waypoints[0][1];
                        Editor.TryBindArrowEndpoint(shape.Id, "start", startX, startY);

                        // Bind end
                        var endX = shape.X + arrowProps.Waypoints[^1][0];
                        var endY = shape.Y + arrowProps.Waypoints[^1][1];
                        Editor.TryBindArrowEndpoint(shape.Id, "end", endX, endY);
                    }
                }
            }

            _tool.CurrentShapeId = null;
            Parent?.Transition("idle");
            Editor.Invalidate();
        }
    }

    // ── Shared state ────────────────────────────────────────

    internal string? CurrentShapeId;
    internal SKPointd StartPoint;
    internal SKPointd EndPoint;
}
