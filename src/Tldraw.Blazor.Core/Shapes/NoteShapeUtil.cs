using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders sticky note shapes — colored rectangle with text.
/// </summary>
public class NoteShapeUtil : ShapeUtil
{
    public override string ShapeType => "note";

    private const double DefaultSize = 200;

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        ShapeType = "note",
        X = x,
        Y = y,
        Width = DefaultSize,
        Height = DefaultSize,
        Style = new TLShapeStyle
        {
            Color = new("#1e1e1e"),
            Fill = new(FillConstants.None),
            StrokeWidth = new(0),
        },
        Props = new TLNoteProps { Text = "", NoteColor = new("#FFEB3B") }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLNoteProps note) return;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        // Note background
        using var fillPaint = new SKPaint
        {
            Color = ParseColor(note.NoteColor, shape.Style.Opacity),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 30),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        var rect = new SKRect(0, 0, (float)shape.Width, (float)shape.Height);

        // Shadow
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(4, 4, (float)shape.Width + 4, (float)shape.Height + 4), 4), shadowPaint);
        // Body
        canvas.DrawRoundRect(new SKRoundRect(rect, 4), fillPaint);

        // Fold corner
        using var foldPaint = new SKPaint
        {
            Color = ParseColor(note.NoteColor, shape.Style.Opacity * 0.7),
            IsAntialias = true,
        };
        float foldSize = 20;
        using var foldPath = new SKPath();
        foldPath.MoveTo((float)shape.Width - foldSize, (float)shape.Height);
        foldPath.LineTo((float)shape.Width, (float)shape.Height - foldSize);
        foldPath.LineTo((float)shape.Width, (float)shape.Height);
        foldPath.Close();
        canvas.DrawPath(foldPath, foldPaint);

        // Text
        if (!string.IsNullOrEmpty(note.Text))
        {
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont { Size = 16 };

            var lines = note.Text.Split('\n');
            float y = 30;
            foreach (var line in lines)
            {
                canvas.DrawText(line, 12, y, SKTextAlign.Left, font, textPaint);
                y += font.Spacing;
            }
        }

        canvas.Restore();
    }

    public override SKRect GetBounds(TLShapeRecord shape) =>
        new((float)shape.X, (float)shape.Y,
            (float)(shape.X + shape.Width), (float)(shape.Y + shape.Height));
}
