using SkiaSharp;
using InkCanvas.Core.Editor;
using InkCanvas.Core.Store;

namespace InkCanvas.Core.Tools;

/// <summary>
/// Arrow tool — drag to create an arrow shape.
/// Child states: Idle → Drawing
/// </summary>
public class ArrowTool : StateNode
{
    public override StateId Id => StateId.Arrow;

    public ArrowTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new DrawingState(this));
    }

    public override void OnEnter()
    {
        Transition(StateId.Idle);
    }

    public class IdleState : StateNode
    {
        private readonly ArrowTool _tool;
        public override StateId Id => StateId.Idle;

        public IdleState(ArrowTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            _tool.StartPoint = new SKPointd(e.WorldX, e.WorldY);
            _tool.EndPoint = _tool.StartPoint;

            var shape = new TLShapeRecord
            {
                Shape = ShapeType.Arrow,
                X = e.WorldX,
                Y = e.WorldY,
                Width = 0,
                Height = 0,
                Style = new TLShapeStyle
                {
                    Color = new("#1e1e1e"),
                    Fill = new(FillConstants.None),
                    StrokeWidth = new(2),
                },
                Props = new TLArrowProps
                {
                    Waypoints = new List<SKPoint>
                    {
                        new(0, 0),
                        new(0, 0)
                    }
                }
            };

            Editor.Store.Put(shape);
            _tool.CurrentShapeId = shape.Id;
            Parent?.Transition(StateId.Drawing);
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
                Editor.SetActiveTool(StateId.Select);
        }
    }

    public class DrawingState : StateNode
    {
        private readonly ArrowTool _tool;
        public override StateId Id => StateId.Drawing;

        public DrawingState(ArrowTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            if (_tool.CurrentShapeId == null) return;

            var shape = Editor.Store.Get<TLShapeRecord>(_tool.CurrentShapeId);
            if (shape?.Props is not TLArrowProps arrow) return;

            _tool.EndPoint = new SKPointd(e.WorldX, e.WorldY);

            // Update last waypoint (relative to shape origin)
            arrow.Waypoints[1] = new SKPoint(
                (float)(e.WorldX - shape.X),
                (float)(e.WorldY - shape.Y));

            // Adjust shape position if end point extends before start
            if (e.WorldX < shape.X)
            {
                shape.Width = shape.X + shape.Width - e.WorldX;
                shape.X = e.WorldX;
                arrow.Waypoints[0] = new SKPoint(
                    (float)(_tool.StartPoint.X - shape.X),
                    arrow.Waypoints[0].Y);
                arrow.Waypoints[1] = new SKPoint(0, arrow.Waypoints[1].Y);
            }
            else
            {
                shape.Width = e.WorldX - shape.X;
            }

            if (e.WorldY < shape.Y)
            {
                shape.Height = shape.Y + shape.Height - e.WorldY;
                shape.Y = e.WorldY;
                arrow.Waypoints[0] = new SKPoint(
                    arrow.Waypoints[0].X,
                    (float)(_tool.StartPoint.Y - shape.Y));
                arrow.Waypoints[1] = new SKPoint(arrow.Waypoints[1].X, 0);
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
                    if (Math.Abs(shape.Width) < 5 && Math.Abs(shape.Height) < 5)
                    {
                        shape.Width = 200;
                        shape.Height = 0;
                        if (shape.Props is TLArrowProps arrow)
                            arrow.Waypoints[1] = new SKPoint(200, 0);
                    }

                    if (shape.Props is TLArrowProps arrowProps && arrowProps.Waypoints.Count >= 2)
                    {
                        var startX = shape.X + arrowProps.Waypoints[0].X;
                        var startY = shape.Y + arrowProps.Waypoints[0].Y;
                        Editor.TryBindArrowEndpoint(shape.Id, ArrowEndpoint.Start, startX, startY);

                        var endX = shape.X + arrowProps.Waypoints[^1].X;
                        var endY = shape.Y + arrowProps.Waypoints[^1].Y;
                        Editor.TryBindArrowEndpoint(shape.Id, ArrowEndpoint.End, endX, endY);
                    }
                }
            }

            _tool.CurrentShapeId = null;
            Parent?.Transition(StateId.Idle);
            Editor.Invalidate();
        }
    }

    internal string? CurrentShapeId;
    internal SKPointd StartPoint;
    internal SKPointd EndPoint;
}
