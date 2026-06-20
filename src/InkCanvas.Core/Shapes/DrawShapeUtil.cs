using SkiaSharp;
using InkCanvas.Core.Store;

namespace InkCanvas.Core.Shapes;

/// <summary>
/// Renders freehand drawing strokes with smooth curves.
/// </summary>
public class DrawShapeUtil : ShapeUtil
{
    public override ShapeType ShapeType => ShapeType.Draw;

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        Shape = ShapeType.Draw,
        X = x,
        Y = y,
        Width = 0,
        Height = 0,
        Style = new TLShapeStyle
        {
            Color = new("#1e1e1e"),
            Fill = new(FillConstants.None),
            StrokeWidth = new(2.5),
        },
        Props = new TLDrawProps()
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLDrawProps draw) return;

        using var paint = MakeStrokePaint(shape.Style, zoom);
        paint.Style = SKPaintStyle.Stroke;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        foreach (var segment in draw.Segments)
        {
            if (segment.Count < 2) continue;
            using var path = CreateSmoothPath(segment);
            canvas.DrawPath(path, paint);
        }

        canvas.Restore();
    }

    /// <summary>Create a smooth SKPath from points using quadratic bezier curves.</summary>
    private static SKPath CreateSmoothPath(List<SKPoint> points)
    {
        var path = new SKPath();
        if (points.Count < 2)
        {
            if (points.Count == 1) path.MoveTo(points[0]);
            return path;
        }

        path.MoveTo(points[0]);

        if (points.Count == 2)
        {
            path.LineTo(points[1]);
            return path;
        }

        for (int i = 1; i < points.Count - 1; i++)
        {
            var prev = points[i - 1];
            var curr = points[i];
            path.QuadTo(prev.X, prev.Y, (prev.X + curr.X) / 2, (prev.Y + curr.Y) / 2);
        }

        path.LineTo(points[^1]);
        return path;
    }

    public override SKRect GetBounds(TLShapeRecord shape)
    {
        if (shape.Props is not TLDrawProps draw || draw.Segments.Count == 0)
            return new SKRect((float)shape.X, (float)shape.Y, (float)shape.X, (float)shape.Y);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var segment in draw.Segments)
        {
            foreach (var pt in segment)
            {
                float px = (float)shape.X + pt.X;
                float py = (float)shape.Y + pt.Y;
                minX = Math.Min(minX, px);
                minY = Math.Min(minY, py);
                maxX = Math.Max(maxX, px);
                maxY = Math.Max(maxY, py);
            }
        }

        if (minX == float.MaxValue)
            return new SKRect((float)shape.X, (float)shape.Y, (float)shape.X, (float)shape.Y);

        float pad = (float)shape.Style.StrokeWidth;
        return new SKRect(minX - pad, minY - pad, maxX + pad, maxY + pad);
    }
}
