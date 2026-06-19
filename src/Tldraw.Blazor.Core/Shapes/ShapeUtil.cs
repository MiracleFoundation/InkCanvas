using SkiaSharp;
using Tldraw.Blazor.Core.Editor;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Abstract base for shape utilities. Each shape type has a ShapeUtil
/// that knows how to create defaults, render, compute bounds, and hit-test.
/// </summary>
public abstract class ShapeUtil
{
    /// <summary>The shape type this util handles (e.g. "geo", "draw").</summary>
    public abstract string ShapeType { get; }

    /// <summary>Create a shape record with default props at the given position.</summary>
    public abstract TLShapeRecord CreateDefault(double x, double y);

    /// <summary>Render the shape onto the canvas.</summary>
    public abstract void Render(SKCanvas canvas, TLShapeRecord shape, float zoom);

    /// <summary>Get the axis-aligned bounding box in world space.</summary>
    public abstract SKRect GetBounds(TLShapeRecord shape);

    /// <summary>Test if a world point hits this shape. Default: bounds contains.</summary>
    public virtual bool HitTest(TLShapeRecord shape, SKPointd worldPoint)
    {
        var bounds = GetBounds(shape);
        return bounds.Contains((float)worldPoint.X, (float)worldPoint.Y);
    }

    // ── Helpers ─────────────────────────────────────────────

    /// <summary>Parse a hex color string to SKColor.</summary>
    protected static SKColor ParseColor(string hex, double opacity = 1.0)
    {
        if (string.IsNullOrEmpty(hex) || hex == "none")
            return SKColors.Transparent;

        hex = hex.TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

        if (hex.Length >= 6 &&
            int.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            var a = (byte)(255 * Math.Clamp(opacity, 0, 1));
            return new SKColor((byte)r, (byte)g, (byte)b, a);
        }

        return SKColors.Black;
    }

    /// <summary>Create an SKPaint for stroke from shape style.</summary>
    protected static SKPaint MakeStrokePaint(TLShapeStyle style, float zoom)
    {
        var paint = new SKPaint
        {
            Color = ParseColor(style.Color, style.Opacity),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)(style.StrokeWidth),
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        if (style.DashPattern == "dashed")
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 8, 4 }, 0);
        else if (style.DashPattern == "dotted")
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 2, 4 }, 0);

        return paint;
    }

    /// <summary>Create an SKPaint for fill from shape style.</summary>
    protected static SKPaint MakeFillPaint(TLShapeStyle style)
    {
        return new SKPaint
        {
            Color = ParseColor(style.Fill, style.Opacity),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
    }
}
