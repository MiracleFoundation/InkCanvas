using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders geometric shapes: rectangle, ellipse, diamond, star, hexagon, triangle.
/// </summary>
public class GeoShapeUtil : ShapeUtil
{
    public override ShapeType ShapeType => ShapeType.Geo;

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        Shape = ShapeType.Geo,
        X = x,
        Y = y,
        Width = 160,
        Height = 100,
        Style = new TLShapeStyle
        {
            Color = new("#1e1e1e"),
            Fill = new("#E8F5E9"),
            StrokeWidth = new(2),
        },
        Props = new TLGeoProps { GeoType = GeoVariant.Rectangle }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        if (shape.Props is not TLGeoProps geo) return;

        using var fillPaint = MakeFillPaint(shape.Style);
        using var strokePaint = MakeStrokePaint(shape.Style, zoom);

        var rect = new SKRect(0, 0, (float)shape.Width, (float)shape.Height);

        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        if (shape.Rotation != 0)
        {
            var cx = (float)shape.Width / 2;
            var cy = (float)shape.Height / 2;
            canvas.RotateDegrees((float)(shape.Rotation * 180 / Math.PI), cx, cy);
        }

        switch (geo.GeoType)
        {
            case GeoVariant.Rectangle:
                DrawRoundedRect(canvas, rect, fillPaint, strokePaint, shape.Style);
                break;
            case GeoVariant.Ellipse:
                canvas.DrawOval(rect, fillPaint);
                canvas.DrawOval(rect, strokePaint);
                break;
            case GeoVariant.Diamond:
                DrawDiamond(canvas, rect, fillPaint, strokePaint);
                break;
            case GeoVariant.Star:
                DrawStar(canvas, rect, fillPaint, strokePaint);
                break;
            case GeoVariant.Hexagon:
                DrawPolygon(canvas, rect, fillPaint, strokePaint, 6);
                break;
            case GeoVariant.Triangle:
                DrawPolygon(canvas, rect, fillPaint, strokePaint, 3);
                break;
            default:
                DrawRoundedRect(canvas, rect, fillPaint, strokePaint, shape.Style);
                break;
        }

        if (!string.IsNullOrEmpty(geo.Text))
            DrawCenteredText(canvas, rect, geo.Text, shape.Style, zoom);

        canvas.Restore();
    }

    public override SKRect GetBounds(TLShapeRecord shape) =>
        new((float)shape.X, (float)shape.Y,
            (float)(shape.X + shape.Width), (float)(shape.Y + shape.Height));

    private static void DrawRoundedRect(SKCanvas canvas, SKRect rect, SKPaint fill, SKPaint stroke, TLShapeStyle style)
    {
        var r = (float)style.BorderRadius;
        if (r > 0)
        {
            canvas.DrawRoundRect(rect, r, r, fill);
            canvas.DrawRoundRect(rect, r, r, stroke);
        }
        else
        {
            canvas.DrawRect(rect, fill);
            canvas.DrawRect(rect, stroke);
        }
    }

    private static void DrawDiamond(SKCanvas canvas, SKRect rect, SKPaint fill, SKPaint stroke)
    {
        using var path = new SKPath();
        path.MoveTo(rect.MidX, rect.Top);
        path.LineTo(rect.Right, rect.MidY);
        path.LineTo(rect.MidX, rect.Bottom);
        path.LineTo(rect.Left, rect.MidY);
        path.Close();
        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, stroke);
    }

    private static void DrawStar(SKCanvas canvas, SKRect rect, SKPaint fill, SKPaint stroke, int points = 5)
    {
        using var path = new SKPath();
        float cx = rect.MidX, cy = rect.MidY;
        float outerRx = rect.Width / 2, outerRy = rect.Height / 2;
        float innerRx = outerRx * 0.4f, innerRy = outerRy * 0.4f;
        double step = Math.PI / points;

        for (int i = 0; i < 2 * points; i++)
        {
            double angle = -Math.PI / 2 + i * step;
            float rx = i % 2 == 0 ? outerRx : innerRx;
            float ry = i % 2 == 0 ? outerRy : innerRy;
            float px = cx + (float)(rx * Math.Cos(angle));
            float py = cy + (float)(ry * Math.Sin(angle));

            if (i == 0) path.MoveTo(px, py);
            else path.LineTo(px, py);
        }
        path.Close();
        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, stroke);
    }

    private static void DrawPolygon(SKCanvas canvas, SKRect rect, SKPaint fill, SKPaint stroke, int sides)
    {
        using var path = new SKPath();
        float cx = rect.MidX, cy = rect.MidY;
        float rx = rect.Width / 2, ry = rect.Height / 2;

        for (int i = 0; i < sides; i++)
        {
            double angle = -Math.PI / 2 + i * 2 * Math.PI / sides;
            float px = cx + (float)(rx * Math.Cos(angle));
            float py = cy + (float)(ry * Math.Sin(angle));
            if (i == 0) path.MoveTo(px, py);
            else path.LineTo(px, py);
        }
        path.Close();
        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, stroke);
    }

    private static void DrawCenteredText(SKCanvas canvas, SKRect rect, string text, TLShapeStyle style, float zoom)
    {
        using var paint = new SKPaint { Color = ParseColor(style.Color), IsAntialias = true };
        using var font = new SKFont { Size = (float)style.FontSize };

        var lines = text.Split('\n');
        float lineHeight = font.Spacing;
        float totalHeight = lineHeight * lines.Length;
        float startY = rect.MidY - totalHeight / 2 + lineHeight * 0.8f;

        foreach (var line in lines)
        {
            var textWidth = font.MeasureText(line);
            float x = rect.MidX - textWidth / 2;
            canvas.DrawText(line, x, startY, SKTextAlign.Left, font, paint);
            startY += lineHeight;
        }
    }
}
