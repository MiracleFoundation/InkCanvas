using SkiaSharp;
using InkCanvas.Core.Editor;
using InkCanvas.Core.Store;

namespace InkCanvas.Core.Tools;

/// <summary>
/// Draw tool — creates freehand strokes by dragging.
/// Child states: Idle → Drawing
/// </summary>
public class DrawTool : StateNode
{
    public override StateId Id => StateId.Draw;

    public DrawTool()
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
        private readonly DrawTool _tool;
        public override StateId Id => StateId.Idle;

        public IdleState(DrawTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            // Create a new draw shape at the pointer position
            var shape = new TLShapeRecord
            {
                Shape = InkCanvas.Core.ShapeType.Draw,
                X = e.WorldX,
                Y = e.WorldY,
                Width = 0,
                Height = 0,
                Style = new TLShapeStyle
                {
                    Color = new("#1e1e1e"),
                    Fill = new(FillConstants.None),
                    StrokeWidth = new(2.5),
                },
                Props = new TLDrawProps
                {
                    Segments = new List<List<SKPoint>>
                    {
                        new() { new SKPoint(0, 0) }
                    }
                }
            };

            Editor.Store.Put(shape);
            _tool.CurrentShapeId = shape.Id;
            _tool.LastWorld = new SKPointd(e.WorldX, e.WorldY);
            Parent?.Transition(StateId.Drawing);
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
            {
                // Switch back to select tool
                Editor.SetActiveTool(StateId.Select);
            }
        }
    }


    public class DrawingState : StateNode
    {
        private readonly DrawTool _tool;
        public override StateId Id => StateId.Drawing;

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
            lastSegment?.Add(new SKPoint((float)relX, (float)relY));

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
            Parent?.Transition(StateId.Idle);
            Editor.Invalidate();
        }
    }


    internal string? CurrentShapeId;
    internal SKPointd LastWorld;
}
