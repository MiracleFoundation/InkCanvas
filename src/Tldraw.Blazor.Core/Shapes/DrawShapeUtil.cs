using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders freehand drawing strokes with smooth curves.
/// </summary>
public class DrawShapeUtil : ShapeUtil
{
    public override string ShapeType => "draw";

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        ShapeType = "draw",
        X = x,
        Y = y,
        Width = 0,
        Height = 0,
        Style = new TLShapeStyle
        {
            Color = "#1e1e1e",
            Fill = "none",
            StrokeWidth = 2.5,
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

    /// <summary>
    /// Create a smooth SKPath from a flat point array using quadratic bezier curves.
    /// Points are in [x1,y1,x2,y2,...] format.
    /// </summary>
    private static SKPath CreateSmoothPath(List<double> points)
    {
        var path = new SKPath();

        if (points.Count < 4)
        {
            // Just 1 point — nothing to draw
            if (points.Count >= 2)
                path.MoveTo((float)points[0], (float)points[1]);
            return path;
        }

        float x0 = (float)points[0], y0 = (float)points[1];
        path.MoveTo(x0, y0);

        if (points.Count == 4)
        {
            // Just 2 points — straight line
            path.LineTo((float)points[2], (float)points[3]);
            return path;
        }

        // For 3+ points, use quadratic bezier through midpoints
        // This creates smooth curves that pass through the midpoints between consecutive points
        float prevX = x0, prevY = y0;

        for (int i = 2; i < points.Count - 2; i += 2)
        {
            float currX = (float)points[i];
            float currY = (float)points[i + 1];

            // Midpoint between previous and current
            float midX = (prevX + currX) / 2;
            float midY = (prevY + currY) / 2;

            // Quadratic bezier: control point = current, end = midpoint
            path.QuadTo(prevX, prevY, midX, midY);

            prevX = currX;
            prevY = currY;
        }

        // Last segment — line to the final point
        float lastX = (float)points[^2];
        float lastY = (float)points[^1];
        path.LineTo(lastX, lastY);

        return path;
    }

    public override SKRect GetBounds(TLShapeRecord shape)
    {
        if (shape.Props is not TLDrawProps draw)
            return new SKRect((float)shape.X, (float)shape.Y, (float)shape.X, (float)shape.Y);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var segment in draw.Segments)
        {
            for (int i = 0; i < segment.Count - 1; i += 2)
            {
                float px = (float)(shape.X + segment[i]);
                float py = (float)(shape.Y + segment[i + 1]);
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
