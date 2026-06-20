using SkiaSharp;
using InkCanvas.Core.Shapes;
using InkCanvas.Core.Store;

namespace InkCanvas.Core.Editor;

/// <summary>
/// Tracks selected shapes and renders selection indicators
/// (outlines, resize handles, rotation handle).
/// </summary>
public class SelectionManager
{
    private readonly HashSet<string> _selectedIds = new();

    /// <summary>Fired when selection changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>Currently selected shape IDs.</summary>
    public IReadOnlyCollection<string> SelectedIds => _selectedIds;

    /// <summary>Number of selected shapes.</summary>
    public int Count => _selectedIds.Count;


    public void Select(string id)
    {
        if (_selectedIds.Add(id))
            SelectionChanged?.Invoke();
    }

    public void Deselect(string id)
    {
        if (_selectedIds.Remove(id))
            SelectionChanged?.Invoke();
    }

    public void Toggle(string id)
    {
        if (_selectedIds.Contains(id))
            _selectedIds.Remove(id);
        else
            _selectedIds.Add(id);
        SelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        if (_selectedIds.Count == 0) return;
        _selectedIds.Clear();
        SelectionChanged?.Invoke();
    }

    public void SelectAll(IEnumerable<string> allIds)
    {
        _selectedIds.Clear();
        foreach (var id in allIds)
            _selectedIds.Add(id);
        SelectionChanged?.Invoke();
    }

    public bool IsSelected(string id) => _selectedIds.Contains(id);

    /// <summary>Get selected shape records from the store.</summary>
    public List<TLShapeRecord> GetSelectedShapes(TLStore store)
    {
        return _selectedIds
            .Select(id => store.Get<TLShapeRecord>(id))
            .Where(s => s != null)
            .Cast<TLShapeRecord>()
            .ToList();
    }


    /// <summary>Resize handle identifiers.</summary>
    public enum HandleType
    {
        TopLeft, TopCenter, TopRight,
        MidLeft, MidRight,
        BottomLeft, BottomCenter, BottomRight,
        Rotation,
        None
    }


    /// <summary>Check if a world point hits any resize/rotation handle.
    /// Returns the handle type, or None.</summary>
    public HandleType HitTestHandles(SKPointd worldPoint, float zoom, ShapeUtilRegistry shapeUtils)
    {
        var combined = GetCombinedBounds(shapeUtils);
        if (combined is null) return HandleType.None;

        var rect = combined.Value;
        float handleHitSize = 12f / zoom; // slightly larger than visual for easier clicking

        var handles = new[]
        {
            (HandleType.TopLeft, new SKPoint(rect.Left, rect.Top)),
            (HandleType.TopCenter, new SKPoint(rect.MidX, rect.Top)),
            (HandleType.TopRight, new SKPoint(rect.Right, rect.Top)),
            (HandleType.MidLeft, new SKPoint(rect.Left, rect.MidY)),
            (HandleType.MidRight, new SKPoint(rect.Right, rect.MidY)),
            (HandleType.BottomLeft, new SKPoint(rect.Left, rect.Bottom)),
            (HandleType.BottomCenter, new SKPoint(rect.MidX, rect.Bottom)),
            (HandleType.BottomRight, new SKPoint(rect.Right, rect.Bottom)),
        };

        foreach (var (type, pos) in handles)
        {
            if (Math.Abs(worldPoint.X - pos.X) <= handleHitSize &&
                Math.Abs(worldPoint.Y - pos.Y) <= handleHitSize)
                return type;
        }

        // Check rotation handle
        var rotY = rect.Top - 20f / zoom;
        var rotCenter = new SKPoint(rect.MidX, rotY);
        float rotHitSize = 10f / zoom;
        if (Math.Abs(worldPoint.X - rotCenter.X) <= rotHitSize &&
            Math.Abs(worldPoint.Y - rotCenter.Y) <= rotHitSize)
            return HandleType.Rotation;

        return HandleType.None;
    }

    /// <summary>Get the combined bounding box of all selected shapes.</summary>
    public SKRect? GetCombinedBounds(ShapeUtilRegistry shapeUtils)
    {
        var shapes = _selectedIds
            .Select(id => GetCurrentShape(id))
            .Where(s => s != null)
            .Cast<TLShapeRecord>()
            .ToList();

        if (shapes.Count == 0) return null;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var shape in shapes)
        {
            var util = shapeUtils.Get(shape.Shape);
            if (util is null) continue;
            var bounds = util.GetBounds(shape);
            minX = Math.Min(minX, bounds.Left);
            minY = Math.Min(minY, bounds.Top);
            maxX = Math.Max(maxX, bounds.Right);
            maxY = Math.Max(maxY, bounds.Bottom);
        }

        if (minX == float.MaxValue) return null;
        return new SKRect(minX, minY, maxX, maxY);
    }


    /// <summary>Render selection outlines and handles for all selected shapes.</summary>
    public void RenderSelection(SKCanvas canvas, float zoom, ShapeUtilRegistry shapeUtils)
    {
        var combined = GetCombinedBounds(shapeUtils);
        if (combined is null) return;

        var rect = combined.Value;

        // Selection outline
        using var outlinePaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x78, 0xD4),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f / zoom,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 6 / zoom, 4 / zoom }, 0),
        };

        // Handle paint
        using var handleFill = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        using var handleStroke = new SKPaint
        {
            Color = new SKColor(0x00, 0x78, 0xD4),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f / zoom,
            IsAntialias = true,
        };

        float handleSize = 8f / zoom;

        // Draw outline
        canvas.DrawRect(rect, outlinePaint);

        // Draw 8 resize handles
        var handlePositions = GetHandlePositions(rect);
        foreach (var handle in handlePositions)
        {
            var hr = new SKRect(
                handle.X - handleSize / 2,
                handle.Y - handleSize / 2,
                handle.X + handleSize / 2,
                handle.Y + handleSize / 2);
            canvas.DrawRect(hr, handleFill);
            canvas.DrawRect(hr, handleStroke);
        }

        // Draw rotation handle
        var rotHandleY = rect.Top - 20f / zoom;
        var rotHandleCenter = new SKPoint(rect.MidX, rotHandleY);
        float rotRadius = 5f / zoom;
        canvas.DrawLine(rect.MidX, rect.Top, rotHandleCenter.X, rotHandleCenter.Y, handleStroke);
        canvas.DrawCircle(rotHandleCenter, rotRadius, handleFill);
        canvas.DrawCircle(rotHandleCenter, rotRadius, handleStroke);
    }

    private static SKPoint[] GetHandlePositions(SKRect rect)
    {
        return
        [
            new SKPoint(rect.Left, rect.Top),
            new SKPoint(rect.MidX, rect.Top),
            new SKPoint(rect.Right, rect.Top),
            new SKPoint(rect.Left, rect.MidY),
            new SKPoint(rect.Right, rect.MidY),
            new SKPoint(rect.Left, rect.Bottom),
            new SKPoint(rect.MidX, rect.Bottom),
            new SKPoint(rect.Right, rect.Bottom),
        ];
    }


    private readonly Dictionary<string, TLShapeRecord> _shapeCache = new();

    public void UpdateShapeCache(TLStore store)
    {
        _shapeCache.Clear();
        foreach (var id in _selectedIds)
        {
            var shape = store.Get<TLShapeRecord>(id);
            if (shape != null)
                _shapeCache[id] = shape;
        }
    }

    private TLShapeRecord? GetCurrentShape(string id) =>
        _shapeCache.TryGetValue(id, out var shape) ? shape : null;
}
