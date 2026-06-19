using SkiaSharp;

namespace Tldraw.Blazor.Core.Editor;

/// <summary>
/// Manages the viewport camera: position (pan) and zoom level.
/// Converts between screen coordinates and world coordinates.
/// </summary>
public class Camera
{
    private const double MinZoom = 0.05;
    private const double MaxZoom = 20.0;

    /// <summary>Camera position in world space (top-left corner of viewport).</summary>
    public SKPointd Position { get; set; } = new(0, 0);

    /// <summary>Zoom level. 1.0 = 100%, 2.0 = 200%, etc.</summary>
    public double Zoom { get; set; } = 1.0;

    /// <summary>Viewport size in screen pixels.</summary>
    public SKSize ViewportSize { get; set; } = new(800, 600);

    /// <summary>Pan the camera by a delta in screen pixels.</summary>
    public void Pan(double screenDx, double screenDy)
    {
        Position = new SKPointd(
            Position.X - screenDx / Zoom,
            Position.Y - screenDy / Zoom);
    }

    /// <summary>
    /// Zoom at a specific screen point.
    /// The world point under the cursor stays fixed after zoom.
    /// </summary>
    public void ZoomAt(SKPoint screenPoint, double delta)
    {
        // World point under cursor before zoom
        var worldBefore = ScreenToWorld(screenPoint);

        // Apply zoom
        Zoom = Math.Clamp(Zoom * (1.0 + delta), MinZoom, MaxZoom);

        // Adjust position so the same world point stays under cursor
        Position = new SKPointd(
            worldBefore.X - screenPoint.X / Zoom,
            worldBefore.Y - screenPoint.Y / Zoom);
    }

    /// <summary>Convert screen coordinates to world coordinates.</summary>
    public SKPointd ScreenToWorld(SKPoint screen)
    {
        return new SKPointd(
            screen.X / Zoom + Position.X,
            screen.Y / Zoom + Position.Y);
    }

    /// <summary>Convert screen coordinates to world coordinates.</summary>
    public SKPointd ScreenToWorld(double screenX, double screenY)
    {
        return ScreenToWorld(new SKPoint((float)screenX, (float)screenY));
    }

    /// <summary>Convert world coordinates to screen coordinates.</summary>
    public SKPoint WorldToScreen(SKPointd world)
    {
        return new SKPoint(
            (float)((world.X - Position.X) * Zoom),
            (float)((world.Y - Position.Y) * Zoom));
    }

    /// <summary>Get the SkiaSharp transform matrix for rendering.</summary>
    public SKMatrix GetTransformMatrix()
    {
        var scale = (float)Zoom;
        var tx = (float)(-Position.X * Zoom);
        var ty = (float)(-Position.Y * Zoom);
        return SKMatrix.CreateScaleTranslation(scale, scale, tx, ty);
    }

    /// <summary>Visible world-space rectangle.</summary>
    public SKRectd GetWorldBounds()
    {
        return new SKRectd(
            Position.X,
            Position.Y,
            Position.X + ViewportSize.Width / Zoom,
            Position.Y + ViewportSize.Height / Zoom);
    }

    /// <summary>Reset camera to default position and zoom.</summary>
    public void Reset()
    {
        Position = new SKPointd(0, 0);
        Zoom = 1.0;
    }
}

/// <summary>Rectangle with double precision (SkiaSharp only has float SKRect).</summary>
public readonly record struct SKRectd(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
}

/// <summary>Point with double precision.</summary>
public readonly record struct SKPointd(double X, double Y);
