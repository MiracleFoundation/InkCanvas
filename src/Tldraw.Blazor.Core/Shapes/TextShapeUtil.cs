using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders text shapes.
/// </summary>
public class TextShapeUtil : ShapeUtil
{
    public override ShapeType ShapeType => ShapeType.Text;

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        Shape = ShapeType.Text,
        X = x,
        Y = y,
        Width = 200,
        Height = 40,
        Style = new TLShapeStyle
        {
            Color = new("#1e1e1e"),
            Fill = new(FillConstants.None),
            StrokeWidth = new(0),
            FontSize = new(24),
        },
        Props = new TLTextProps { Text = string.Empty, FontSize = new(24) }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLTextProps text) return;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        using var paint = new SKPaint
        {
            Color = ParseColor(shape.Style.Color, shape.Style.Opacity),
            IsAntialias = true,
        };

        var fontSize = (float)(text.FontSize > 0 ? text.FontSize : shape.Style.FontSize);
        using var font = new SKFont { Size = fontSize };

        var lines = text.Text.Split('\n');
        float lineHeight = font.Spacing;
        float y = lineHeight;

        float maxWidth = 0;
        foreach (var line in lines)
        {
            var w = font.MeasureText(line);
            if (w > maxWidth) maxWidth = w;
        }

        shape.Width = maxWidth + 8;
        shape.Height = lineHeight * lines.Length + 4;

        foreach (var line in lines)
        {
            float x = text.TextAlign switch
            {
                TextAlign.Center => (float)shape.Width / 2 - font.MeasureText(line) / 2,
                TextAlign.Right => (float)shape.Width - font.MeasureText(line),
                _ => 0
            };
            canvas.DrawText(line, x, y, SKTextAlign.Left, font, paint);
            y += lineHeight;
        }

        canvas.Restore();
    }

    public override SKRect GetBounds(TLShapeRecord shape) =>
        new((float)shape.X, (float)shape.Y,
            (float)(shape.X + shape.Width), (float)(shape.Y + shape.Height));
}
