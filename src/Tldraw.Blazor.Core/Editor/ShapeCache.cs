using SkiaSharp;
using Tldraw.Blazor.Core.Shapes;
using Tldraw.Blazor.Core.Store;

namespace Tldraw.Blazor.Core.Editor;

/// <summary>
/// Caches rendered shapes as SKBitmaps for performance.
/// Shapes are cached individually and composited during rendering.
/// Only re-renders shapes that have changed since last frame.
/// </summary>
public class ShapeCache
{
    private readonly Dictionary<string, CachedShape> _cache = new();

    /// <summary>Enable/disable caching. Disabled by default for simplicity.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum cache size (number of cached shapes).</summary>
    public int MaxCacheSize { get; set; } = 200;

    /// <summary>
    /// Invalidate cache for a specific shape (call when shape changes).
    /// </summary>
    public void Invalidate(string shapeId)
    {
        if (_cache.Remove(shapeId, out var cached))
        {
            cached.Bitmap?.Dispose();
        }
    }

    /// <summary>
    /// Invalidate all cached shapes.
    /// </summary>
    public void InvalidateAll()
    {
        foreach (var cached in _cache.Values)
            cached.Bitmap?.Dispose();
        _cache.Clear();
    }

    /// <summary>
    /// Render a shape, using cache if available.
    /// Falls back to direct rendering if caching is disabled or fails.
    /// </summary>
    public void RenderShape(SKCanvas canvas, TLShapeRecord shape, ShapeUtil util, float zoom)
    {
        if (!Enabled)
        {
            util.Render(canvas, shape, zoom);
            return;
        }

        var cacheKey = GetCacheKey(shape, zoom);

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.Version == shapeVersion(shape))
        {
            // Use cached bitmap
            if (cached.Bitmap != null)
            {
                canvas.DrawBitmap(cached.Bitmap, 0, 0);
            }
            else
            {
                util.Render(canvas, shape, zoom);
            }
        }
        else
        {
            // Render to cache
            RenderToCache(canvas, shape, util, zoom, cacheKey);
        }
    }

    private void RenderToCache(SKCanvas canvas, TLShapeRecord shape, ShapeUtil util, float zoom, string cacheKey)
    {
        var bounds = util.GetBounds(shape);
        var padding = (float)shape.Style.StrokeWidth + 4;
        var width = (int)(bounds.Width + padding * 2);
        var height = (int)(bounds.Height + padding * 2);

        if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
        {
            // Too small or too large — render directly
            util.Render(canvas, shape, zoom);
            return;
        }

        try
        {
            var bitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));

            if (surface == null)
            {
                bitmap.Dispose();
                util.Render(canvas, shape, zoom);
                return;
            }

            var cacheCanvas = surface.Canvas;
            cacheCanvas.Clear(SKColors.Transparent);
            cacheCanvas.Translate(-bounds.Left + padding, -bounds.Top + padding);
            util.Render(cacheCanvas, shape, zoom);

            // Copy surface to bitmap
            using var image = surface.Snapshot();
            using var pixmap = new SKPixmap();
            if (image.PeekPixels(pixmap))
            {
                bitmap.InstallPixels(pixmap);
            }

            // Store in cache
            if (_cache.Count >= MaxCacheSize)
                EvictOldest();

            _cache[cacheKey] = new CachedShape
            {
                Bitmap = bitmap,
                Version = shapeVersion(shape),
                Bounds = new SKRect(bounds.Left - padding, bounds.Top - padding,
                                   bounds.Right + padding, bounds.Bottom + padding),
                LastUsed = DateTime.UtcNow,
            };

            // Draw the cached bitmap
            canvas.DrawBitmap(bitmap, bounds.Left - padding, bounds.Top - padding);
        }
        catch
        {
            // Fallback to direct rendering
            util.Render(canvas, shape, zoom);
        }
    }

    private static string GetCacheKey(TLShapeRecord shape, float zoom)
    {
        // Include zoom level in cache key (different zoom = different rendering)
        return $"{shape.Id}:{Math.Round(zoom, 2)}";
    }

    private static int shapeVersion(TLShapeRecord shape)
    {
        // Simple version based on hash of key properties (chain to support >8 args)
        var h1 = HashCode.Combine(
            shape.X, shape.Y, shape.Width, shape.Height, shape.Rotation,
            shape.Style.Color, shape.Style.Fill, shape.Style.StrokeWidth);
        var h2 = HashCode.Combine(h1, shape.Style.Opacity, shape.Style.DashPattern);
        return h2;
    }

    private void EvictOldest()
    {
        if (_cache.Count == 0) return;

        var oldest = _cache.MinBy(kvp => kvp.Value.LastUsed);
        if (oldest.Value != null)
        {
            oldest.Value.Bitmap?.Dispose();
            _cache.Remove(oldest.Key);
        }
    }

    private class CachedShape
    {
        public SKBitmap? Bitmap { get; set; }
        public int Version { get; set; }
        public SKRect Bounds { get; set; }
        public DateTime LastUsed { get; set; }
    }
}
