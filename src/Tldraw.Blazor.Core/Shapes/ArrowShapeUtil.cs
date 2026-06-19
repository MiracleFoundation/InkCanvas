using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders arrow shapes with arrowhead and binding indicators.
/// </summary>
public class ArrowShapeUtil : ShapeUtil
{
    public override string ShapeType => "arrow";

    /// <summary>Store reference for checking bindings (set by Editor).</summary>
    public TLStore? Store { get; set; }

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        ShapeType = "arrow",
        X = x,
        Y = y,
        Width = 200,
        Height = 0,
        Style = new TLShapeStyle
        {
            Color = "#1e1e1e",
            Fill = "none",
            StrokeWidth = 2,
        },
        Props = new TLArrowProps
        {
            Waypoints = new List<List<double>>
            {
                new() { 0, 0 },
                new() { 200, 0 }
            }
        }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLArrowProps arrow || arrow.Waypoints.Count < 2) return;

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        using var paint = MakeStrokePaint(shape.Style, zoom);

        var startX = (float)arrow.Waypoints[0][0];
        var startY = (float)arrow.Waypoints[0][1];
        var endX = (float)arrow.Waypoints[^1][0];
        var endY = (float)arrow.Waypoints[^1][1];

        // Draw line segments
        using var path = new SKPath();
        path.MoveTo(startX, startY);

        if (arrow.Waypoints.Count == 2)
        {
            path.LineTo(endX, endY);
        }
        else
        {
            for (int i = 1; i < arrow.Waypoints.Count; i++)
            {
                var pt = arrow.Waypoints[i];
                path.LineTo((float)pt[0], (float)pt[1]);
            }
        }

        canvas.DrawPath(path, paint);

        // Draw arrowhead
        DrawArrowhead(canvas, endX, endY, startX, startY, paint, zoom);

        // Draw endpoint dots
        using var dotPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x78, 0xD4),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        float dotRadius = 4f / zoom;

        // Check if endpoints are bound (green = bound, blue = unbound)
        bool startBound = IsEndpointBound(shape.Id, "start");
        bool endBound = IsEndpointBound(shape.Id, "end");

        using var boundPaint = new SKPaint
        {
            Color = new SKColor(0x22, 0xC5, 0x5E), // green
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        canvas.DrawCircle(startX, startY, dotRadius, startBound ? boundPaint : dotPaint);
        canvas.DrawCircle(endX, endY, dotRadius, endBound ? boundPaint : dotPaint);

        // Draw binding target highlight if bound
        if (startBound)
            DrawBindingHighlight(canvas, shape.Id, "start", zoom);
        if (endBound)
            DrawBindingHighlight(canvas, shape.Id, "end", zoom);

        canvas.Restore();
    }

    private bool IsEndpointBound(string arrowId, string endpoint)
    {
        if (Store == null) return false;
        var bindingId = $"binding:{arrowId}:{endpoint}";
        return Store.Get(bindingId) != null;
    }

    private void DrawBindingHighlight(SKCanvas canvas, string arrowId, string endpoint, float zoom)
    {
        // Green dot at endpoint is sufficient visual feedback
        // Full shape highlight would require coordinate transformation
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
            if (pt.Count < 2) continue;
            float px = (float)(shape.X + pt[0]);
            float py = (float)(shape.Y + pt[1]);
            minX = Math.Min(minX, px);
            minY = Math.Min(minY, py);
            maxX = Math.Max(maxX, px);
            maxY = Math.Max(maxY, py);
        }

        float pad = (float)shape.Style.StrokeWidth + 6;
        return new SKRect(minX - pad, minY - pad, maxX + pad, maxY + pad);
    }
}
