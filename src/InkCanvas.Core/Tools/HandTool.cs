using SkiaSharp;

namespace InkCanvas.Core.Tools;

/// <summary>
/// Hand tool — pans the canvas by dragging.
/// Also accessible by holding Space in any tool.
/// </summary>
public class HandTool : StateNode
{
    public override StateId Id => StateId.Hand;

    public HandTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new PanningState(this));
    }

    public override void OnEnter()
    {
        Transition(StateId.Idle);
    }


    public class IdleState : StateNode
    {
        private readonly HandTool _tool;
        public override StateId Id => StateId.Idle;

        public IdleState(HandTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            _tool.LastScreen = new SKPoint((float)e.ScreenX, (float)e.ScreenY);
            Parent?.Transition(StateId.Panning);
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
                Editor.SetActiveTool(StateId.Select);
        }
    }


    public class PanningState : StateNode
    {
        private readonly HandTool _tool;
        public override StateId Id => StateId.Panning;

        public PanningState(HandTool tool) => _tool = tool;

        public override void OnPointerMove(PointerEvent e)
        {
            var current = new SKPoint((float)e.ScreenX, (float)e.ScreenY);
            var dx = current.X - _tool.LastScreen.X;
            var dy = current.Y - _tool.LastScreen.Y;

            Editor.Camera.Pan(dx, dy);
            _tool.LastScreen = current;
            Editor.Invalidate();
        }

        public override void OnPointerUp(PointerEvent e)
        {
            Parent?.Transition(StateId.Idle);
        }
    }


    internal SKPoint LastScreen;
}
