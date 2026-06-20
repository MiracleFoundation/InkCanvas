using InkCanvas.Core.Editor;

namespace InkCanvas.Core.Tools;

/// <summary>
/// Eraser tool — click or drag over shapes to delete them.
/// Child states: Idle → Erasing
/// </summary>
public class EraserTool : StateNode
{
    public override StateId Id => StateId.Eraser;

    /// <summary>Shapes queued for deletion (erased during drag).</summary>
    public HashSet<string> ErasedIds { get; } = new();

    public EraserTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new ErasingState(this));
    }

    public override void OnEnter()
    {
        ErasedIds.Clear();
        Transition(StateId.Idle);
    }


    public class IdleState : StateNode
    {
        private readonly EraserTool _tool;
        public override StateId Id => StateId.Idle;

        public IdleState(EraserTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            _tool.ErasedIds.Clear();
            EraseAtPoint(e.WorldX, e.WorldY);
            Parent?.Transition(StateId.Erasing);
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
                Editor.SetActiveTool(StateId.Select);
        }

        private void EraseAtPoint(double worldX, double worldY)
        {
            var shapes = Editor.Store.GetPageShapes();
            var point = new SKPointd(worldX, worldY);

            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                var shape = shapes[i];
                if (shape.IsLocked || shape.IsHidden) continue;
                if (_tool.ErasedIds.Contains(shape.Id)) continue;

                var util = Editor.ShapeUtils.Get(shape.Shape);
                if (util != null && util.HitTest(shape, point))
                {
                    _tool.ErasedIds.Add(shape.Id);
                    Editor.Store.Remove(shape.Id);
                    Editor.Selection.Deselect(shape.Id);
                    break; // erase one shape per click
                }
            }
        }
    }


    public class ErasingState : StateNode
    {
        private readonly EraserTool _tool;
        public override StateId Id => StateId.Erasing;

        public ErasingState(EraserTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            EraseAtPoint(e.WorldX, e.WorldY);
            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            _tool.ErasedIds.Clear();
            Parent?.Transition(StateId.Idle);
            Editor.Invalidate();
        }

        private void EraseAtPoint(double worldX, double worldY)
        {
            var shapes = Editor.Store.GetPageShapes();
            var point = new SKPointd(worldX, worldY);

            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                var shape = shapes[i];
                if (shape.IsLocked || shape.IsHidden) continue;
                if (_tool.ErasedIds.Contains(shape.Id)) continue;

                var util = Editor.ShapeUtils.Get(shape.Shape);
                if (util != null && util.HitTest(shape, point))
                {
                    _tool.ErasedIds.Add(shape.Id);
                    Editor.Store.Remove(shape.Id);
                    Editor.Selection.Deselect(shape.Id);
                    break;
                }
            }
        }
    }
}
