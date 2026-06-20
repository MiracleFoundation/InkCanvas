using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders line shapes — straight line or polyline.
/// </summary>
public class LineShapeUtil : ShapeUtil
{
    public override string ShapeType => "line";

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        ShapeType = "line",
        X = x,
        Y = y,
        Width = 200,
        Height = 0,
        Style = new TLShapeStyle
        {
            Color = new("#1e1e1e"),
            Fill = new(FillConstants.None),
            StrokeWidth = new(2),
        },
        Props = new TLLineProps
        {
            Points = new List<SKPoint>
            {
                new(0, 0),
                new(200, 0)
            }
        }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLLineProps line || line.Points.Count < 2) return;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        using var paint = MakeStrokePaint(shape.Style, zoom);

        using var path = new SKPath();
        path.MoveTo(line.Points[0]);
        for (int i = 1; i < line.Points.Count; i++)
            path.LineTo(line.Points[i]);

        canvas.DrawPath(path, paint);
        canvas.Restore();
    }

    public override SKRect GetBounds(TLShapeRecord shape)
    {
        if (shape.Props is not TLLineProps line || line.Points.Count == 0)
            return new SKRect((float)shape.X, (float)shape.Y, (float)shape.X, (float)shape.Y);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var pt in line.Points)
        {
            float px = (float)shape.X + pt.X;
            float py = (float)shape.Y + pt.Y;
            minX = Math.Min(minX, px);
            minY = Math.Min(minY, py);
            maxX = Math.Max(maxX, px);
            maxY = Math.Max(maxY, py);
        }

        float pad = (float)shape.Style.StrokeWidth;
        return new SKRect(minX - pad, minY - pad, maxX + pad, maxY + pad);
    }
}
