using SkiaSharp;
using InkCanvas.Core.Store;

namespace InkCanvas.Core.Shapes;

/// <summary>
/// Renders frame containers — bounding rectangle with label.
/// </summary>
public class FrameShapeUtil : ShapeUtil
{
    public override ShapeType ShapeType => ShapeType.Frame;

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        Shape = ShapeType.Frame,
        X = x,
        Y = y,
        Width = 400,
        Height = 300,
        Style = new TLShapeStyle
        {
            Color = new("#888888"),
            Fill = new("#FFFFFF"),
            StrokeWidth = new(1.5),
        },
        Props = new TLFrameProps { Name = "Frame" }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLFrameProps frame) return;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        var rect = new SKRect(0, 0, (float)shape.Width, (float)shape.Height);

        // Background
        using var fillPaint = MakeFillPaint(shape.Style);
        canvas.DrawRect(rect, fillPaint);

        // Border
        using var strokePaint = MakeStrokePaint(shape.Style, zoom);
        canvas.DrawRect(rect, strokePaint);

        // Label bar
        using var labelBgPaint = new SKPaint
        {
            Color = new SKColor(0xF5, 0xF5, 0xF5),
            IsAntialias = true,
        };
        var labelHeight = 24f / zoom;
        canvas.DrawRect(new SKRect(0, -labelHeight, (float)shape.Width, 0), labelBgPaint);

        // Label text
        using var textPaint = new SKPaint { Color = SKColors.DarkGray, IsAntialias = true };
        using var font = new SKFont { Size = 12f / zoom };
        canvas.DrawText(frame.Name, 4 / zoom, -6 / zoom, SKTextAlign.Left, font, textPaint);

        canvas.Restore();
    }

    public override SKRect GetBounds(TLShapeRecord shape) =>
        new((float)shape.X, (float)shape.Y,
            (float)(shape.X + shape.Width), (float)(shape.Y + shape.Height));
}
