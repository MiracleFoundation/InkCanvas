using SkiaSharp;
using InkCanvas.Core.Store;

namespace InkCanvas.Core.Shapes;

/// <summary>
/// Renders arrow shapes with arrowhead and binding indicators.
/// </summary>
public class ArrowShapeUtil : ShapeUtil
{
    public override ShapeType ShapeType => ShapeType.Arrow;

    /// <summary>Store reference for checking bindings (set by Editor).</summary>
    public TLStore? Store { get; set; }

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        Shape = ShapeType.Arrow,
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
        Props = new TLArrowProps
        {
            Waypoints = new List<SKPoint>
            {
                new(0, 0),
                new(200, 0)
            }
        }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLArrowProps arrow || arrow.Waypoints.Count < 2) return;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        using var paint = MakeStrokePaint(shape.Style, zoom);

        var start = arrow.Waypoints[0];
        var end = arrow.Waypoints[^1];

        // Draw line segments
        using var path = new SKPath();
        path.MoveTo(start);

        if (arrow.Waypoints.Count == 2)
        {
            path.LineTo(end);
        }
        else
        {
            for (int i = 1; i < arrow.Waypoints.Count; i++)
                path.LineTo(arrow.Waypoints[i]);
        }

        canvas.DrawPath(path, paint);

        // Draw arrowhead
        DrawArrowhead(canvas, end.X, end.Y, start.X, start.Y, paint, zoom);

        // Draw endpoint dots
        using var dotPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x78, 0xD4),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        float dotRadius = 4f / zoom;

        bool startBound = IsEndpointBound(shape.Id, ArrowEndpoint.Start);
        bool endBound = IsEndpointBound(shape.Id, ArrowEndpoint.End);

        using var boundPaint = new SKPaint
        {
            Color = new SKColor(0x22, 0xC5, 0x5E),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        canvas.DrawCircle(start, dotRadius, startBound ? boundPaint : dotPaint);
        canvas.DrawCircle(end, dotRadius, endBound ? boundPaint : dotPaint);

        canvas.Restore();
    }

    private bool IsEndpointBound(string arrowId, ArrowEndpoint endpoint)
    {
        if (Store == null) return false;
        var bindingId = $"binding:{arrowId}:{endpoint.ToValue()}";
        return Store.Get(bindingId) != null;
    }

    private static void DrawArrowhead(SKCanvas canvas, float tipX, float tipY,
        float fromX, float fromY, SKPaint paint, float zoom)
    {
        float headLen = 12f / zoom;
        float headAngle = 0.5f;

        float angle = MathF.Atan2(tipY - fromY, tipX - fromX);

        float x1 = tipX - headLen * MathF.Cos(angle - headAngle);
        float y1 = tipY - headLen * MathF.Sin(angle - headAngle);
        float x2 = tipX - headLen * MathF.Cos(angle + headAngle);
        float y2 = tipY - headLen * MathF.Sin(angle + headAngle);

        using var headPath = new SKPath();
        headPath.MoveTo(tipX, tipY);
        headPath.LineTo(x1, y1);
        headPath.MoveTo(tipX, tipY);
        headPath.LineTo(x2, y2);

        canvas.DrawPath(headPath, paint);
    }

    public override SKRect GetBounds(TLShapeRecord shape)
    {
        if (shape.Props is not TLArrowProps arrow || arrow.Waypoints.Count == 0)
            return new SKRect((float)shape.X, (float)shape.Y, (float)shape.X, (float)shape.Y);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var pt in arrow.Waypoints)
        {
            float px = (float)shape.X + pt.X;
            float py = (float)shape.Y + pt.Y;
            minX = Math.Min(minX, px);
            minY = Math.Min(minY, py);
            maxX = Math.Max(maxX, px);
            maxY = Math.Max(maxY, py);
        }

        float pad = (float)shape.Style.StrokeWidth + 6;
        return new SKRect(minX - pad, minY - pad, maxX + pad, maxY + pad);
    }
}
