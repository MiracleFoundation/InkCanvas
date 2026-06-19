using SkiaSharp;
using Tldraw.Blazor.Core.Store;
using Editor = Tldraw.Blazor.Core.Editor.Editor;

namespace Tldraw.Blazor.Core.Tools;

/// <summary>
/// Text tool — click to place a text shape, then type to edit.
/// Child states: Idle → Editing
/// </summary>
public class TextTool : StateNode
{
    public override string Id => "text";

    /// <summary>ID of the shape currently being edited, if any.</summary>
    public string? EditingShapeId { get; set; }

    public TextTool()
    {
        RegisterChild(new IdleState(this));
        RegisterChild(new EditingState(this));
    }

    public override void OnEnter()
    {
        EditingShapeId = null;
        Transition("idle");
    }

    // ── Idle State ──────────────────────────────────────────

    public class IdleState : StateNode
    {
        private readonly TextTool _tool;
        public override string Id => "idle";

        public IdleState(TextTool tool) => _tool = tool;

        public override void OnPointerDown(PointerEvent e)
        {
            // Create a new text shape at click position
            var shape = new TLShapeRecord
            {
                ShapeType = "text",
                X = e.WorldX,
                Y = e.WorldY,
                Width = 200,
                Height = 40,
                Style = new TLShapeStyle
                {
                    Color = "#1e1e1e",
                    Fill = "none",
                    StrokeWidth = 0,
                    FontSize = 24,
                },
                Props = new TLTextProps
                {
                    Text = "",
                    FontSize = 24,
                    TextAlign = "left",
                }
            };

            Editor.Store.Put(shape);
            Editor.Selection.ClearSelection();
            Editor.Selection.Select(shape.Id);

            _tool.EditingShapeId = shape.Id;
            Parent?.Transition("editing");
            Editor.Invalidate();
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (e.Key == "Escape")
                Editor.SetActiveTool("select");
        }
    }

    // ── Editing State ───────────────────────────────────────

    public class EditingState : StateNode
    {
        private readonly TextTool _tool;
        public override string Id => "editing";

        public EditingState(TextTool tool) => _tool = tool;

        public override void OnEnter()
        {
            _tool.EditingShapeId = _tool.EditingShapeId;
        }

        public override void OnKeyDown(KeyEvent e)
        {
            if (_tool.EditingShapeId == null) return;

            var shape = Editor.Store.Get<TLShapeRecord>(_tool.EditingShapeId);
            if (shape?.Props is not TLTextProps text) return;

            if (e.Key == "Escape")
            {
                // Finish editing
                if (string.IsNullOrEmpty(text.Text))
                {
                    // Delete empty text shape
                    Editor.Store.Remove(shape.Id);
                    Editor.Selection.ClearSelection();
                }
                _tool.EditingShapeId = null;
                Parent?.Transition("idle");
                Editor.Invalidate();
                return;
            }

            if (e.Key == "Enter" && !e.ShiftKey)
            {
                // Finish editing on Enter (Shift+Enter = newline)
                _tool.EditingShapeId = null;
                Parent?.Transition("idle");
                Editor.Invalidate();
                return;
            }

            if (e.Key == "Backspace")
            {
                if (text.Text.Length > 0)
                    text.Text = text.Text[..^1];
            }
            else if (e.Key == "Enter" && e.ShiftKey)
            {
                text.Text += "\n";
            }
            else if (e.Key.Length == 1 && !e.CtrlKey && !e.AltKey)
            {
                text.Text += e.Key;
            }

            Editor.Invalidate();
        }

        public override void OnPointerDown(PointerEvent e)
        {
            // Click outside → finish editing
            if (_tool.EditingShapeId != null)
            {
                var shape = Editor.Store.Get<TLShapeRecord>(_tool.EditingShapeId);
                if (shape?.Props is TLTextProps text && string.IsNullOrEmpty(text.Text))
                {
                    Editor.Store.Remove(shape.Id);
                    Editor.Selection.ClearSelection();
                }
            }

            _tool.EditingShapeId = null;
            Parent?.Transition("idle");
            Editor.Invalidate();
        }

        public override void Render(SKCanvas canvas, float zoom)
        {
            if (_tool.EditingShapeId == null) return;

            var shape = Editor.Store.Get<TLShapeRecord>(_tool.EditingShapeId);
            if (shape == null) return;

            // Draw editing cursor (blinking caret)
            var cursorX = (float)shape.X;
            var cursorY = (float)shape.Y;

            if (shape.Props is TLTextProps text)
            {
                using var font = new SKFont { Size = (float)text.FontSize };
                var lines = text.Text.Split('\n');
                if (lines.Length > 0)
                {
                    var lastLine = lines[^1];
                    cursorX += font.MeasureText(lastLine);
                    cursorY += font.Spacing * (lines.Length - 1);
                }
            }

            using var caretPaint = new SKPaint
            {
                Color = new SKColor(0x00, 0x78, 0xD4),
                StrokeWidth = 2f / zoom,
                IsAntialias = true,
            };

            float caretHeight = 20f;
            canvas.DrawLine(
                (float)cursorX, (float)cursorY,
                (float)cursorX, (float)cursorY + caretHeight,
                caretPaint);
        }
    }
}
