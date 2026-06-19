using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders text shapes.
/// </summary>
public class TextShapeUtil : ShapeUtil
{
    public override string ShapeType => "text";

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        ShapeType = "text",
        X = x,
        Y = y,
        Width = 200,
        Height = 40,
        Style = new TLShapeStyle
        {
            Color = "#1e1e1e",
            Fill = "none",
            StrokeWidth = 0,
            FontSize = 24,
        },
        Props = new TLTextProps { Text = "Text", FontSize = 24 }
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
        float y = lineHeight; // first line baseline

        float maxWidth = 0;
        foreach (var line in lines)
        {
            var w = font.MeasureText(line);
            if (w > maxWidth) maxWidth = w;
        }

        // Update shape width/height to fit text
        shape.Width = maxWidth + 8;
        shape.Height = lineHeight * lines.Length + 4;

        foreach (var line in lines)
        {
            float x = text.TextAlign switch
            {
                "center" => (float)shape.Width / 2 - font.MeasureText(line) / 2,
                "right" => (float)shape.Width - font.MeasureText(line),
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
