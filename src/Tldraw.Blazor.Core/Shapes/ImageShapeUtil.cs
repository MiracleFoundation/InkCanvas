using SkiaSharp;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Renders image shapes. Images are loaded as SKBitmap and cached.
/// </summary>
public class ImageShapeUtil : ShapeUtil
{
    public override string ShapeType => "image";

    /// <summary>Cache of loaded images by asset ID.</summary>
    private static readonly Dictionary<string, SKBitmap> _imageCache = new();

    public override TLShapeRecord CreateDefault(double x, double y) => new()
    {
        ShapeType = "image",
        X = x,
        Y = y,
        Width = 200,
        Height = 200,
        Style = new TLShapeStyle { Color = new("#ccc"), Fill = new(FillConstants.None), StrokeWidth = new(1) },
        Props = new TLImageProps { AssetId = "" }
    };

    public override void Render(SKCanvas canvas, TLShapeRecord shape, float zoom)
    {
        canvas.Save();
        canvas.Translate((float)shape.X, (float)shape.Y);

        var rect = new SKRect(0, 0, (float)shape.Width, (float)shape.Height);

        if (shape.Props is TLImageProps img && !string.IsNullOrEmpty(img.AssetId))
        {
            if (_imageCache.TryGetValue(img.AssetId, out var bitmap))
            {
                // Draw the image
                canvas.DrawBitmap(bitmap, rect);
            }
            else
            {
                // Placeholder — image not loaded
                DrawPlaceholder(canvas, rect, shape.Style, zoom);
            }
        }
        else
        {
            DrawPlaceholder(canvas, rect, shape.Style, zoom);
        }

        canvas.Restore();
    }

    private static void DrawPlaceholder(SKCanvas canvas, SKRect rect, TLShapeStyle style, float zoom)
    {
        using var fillPaint = new SKPaint
        {
            Color = new SKColor(0xF5, 0xF5, 0xF5),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        using var strokePaint = new SKPaint
        {
            Color = new SKColor(0xDD, 0xDD, 0xDD),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f / zoom,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 4 / zoom, 4 / zoom }, 0),
        };

        canvas.DrawRect(rect, fillPaint);
        canvas.DrawRect(rect, strokePaint);

        // Draw image icon (mountain/sun)
        using var iconPaint = new SKPaint
        {
            Color = new SKColor(0xBB, 0xBB, 0xBB),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        float cx = rect.MidX, cy = rect.MidY;
        float s = Math.Min(rect.Width, rect.Height) * 0.3f;

        // Sun
        canvas.DrawCircle(cx - s * 0.4f, cy - s * 0.3f, s * 0.2f, iconPaint);

        // Mountain
        using var mountainPath = new SKPath();
        mountainPath.MoveTo(cx - s * 0.6f, cy + s * 0.4f);
        mountainPath.LineTo(cx - s * 0.1f, cy - s * 0.4f);
        mountainPath.LineTo(cx + s * 0.2f, cy);
        mountainPath.LineTo(cx + s * 0.4f, cy - s * 0.2f);
        mountainPath.LineTo(cx + s * 0.6f, cy + s * 0.4f);
        mountainPath.Close();
        canvas.DrawPath(mountainPath, iconPaint);
    }

    /// <summary>Load an image from bytes into the cache.</summary>
    public static void LoadImage(string assetId, byte[] imageBytes)
    {
        var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap != null)
            _imageCache[assetId] = bitmap;
    }

    /// <summary>Load an image from a data URI (base64).</summary>
    public static void LoadImageFromDataUri(string assetId, string dataUri)
    {
        // data:image/png;base64,xxxxx
        var commaIndex = dataUri.IndexOf(',');
        if (commaIndex < 0) return;

        var base64 = dataUri[(commaIndex + 1)..];
        var bytes = Convert.FromBase64String(base64);
        LoadImage(assetId, bytes);
    }

    public override SKRect GetBounds(TLShapeRecord shape) =>
        new((float)shape.X, (float)shape.Y,
            (float)(shape.X + shape.Width), (float)(shape.Y + shape.Height));
}
