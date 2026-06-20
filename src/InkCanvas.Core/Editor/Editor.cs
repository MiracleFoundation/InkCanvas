using SkiaSharp;
using InkCanvas.Core.Shapes;
using InkCanvas.Core.Store;
using InkCanvas.Core.Tools;

namespace InkCanvas.Core.Editor;

/// <summary>
/// Central editor engine. Holds camera, store, shape registry,
/// selection, and active tool. Manages state and rendering.
/// </summary>
public class Editor
{
    public Camera Camera { get; } = new();

    /// <summary>The reactive record store holding all editor state.</summary>
    public TLStore Store { get; } = new();

    /// <summary>Shape rendering cache for performance.</summary>
    public ShapeCache ShapeCache { get; } = new();

    /// <summary>Current active page ID.</summary>
    public string CurrentPageId { get; private set; } = "page:default";

    /// <summary>Initialize default page if none exists.</summary>
    public void EnsureDefaultPage()
    {
        if (Store.GetPages().Count == 0)
        {
            Store.Put(new TLPageRecord { Id = "page:default", Name = "Page 1", Index = 0 });
        }
    }

    /// <summary>Registry of shape util implementations.</summary>
    public ShapeUtilRegistry ShapeUtils { get; } = new();

    /// <summary>Selection manager — tracks selected shapes.</summary>
    public SelectionManager Selection { get; } = new();

    /// <summary>Undo/redo history manager.</summary>
    public HistoryManager History { get; } = new();

    /// <summary>Clipboard for copy/paste (shape snapshot JSON).</summary>
    private string? _clipboardJson;

    /// <summary>All registered tools by ID.</summary>
    private readonly Dictionary<StateId, StateNode> _tools = new();

    /// <summary>Currently active tool (root state node).</summary>
    public StateNode ActiveTool { get; private set; } = null!;

    /// <summary>Current pointer position in screen coordinates.</summary>
    public SKPoint PointerScreen { get; private set; }

    /// <summary>Current pointer position in world coordinates.</summary>
    public SKPointd PointerWorld { get; private set; }

    /// <summary>Whether the pointer is currently down.</summary>
    public bool IsPointerDown { get; private set; }

    /// <summary>Whether the Space key is held (for temporary panning).</summary>
    public bool IsSpaceHeld { get; private set; }

    /// <summary>Last screen position for space+drag panning.</summary>
    private SKPoint _panLastScreen;

    /// <summary>Currently hovered resize handle (for cursor display).</summary>
    public SelectionManager.HandleType HoveredHandle { get; private set; }

    /// <summary>Fired when state changes and a re-render is needed.</summary>
    public event Action? StateChanged;

    public Editor()
    {
        // Re-render when store changes
        Store.Changed += changes =>
        {
            // Invalidate cache for changed shapes
            foreach (var change in changes)
            {
                if (change.Record is TLShapeRecord)
                    ShapeCache.Invalidate(change.RecordId);
            }
            Invalidate();
        };

        // Re-render when selection changes
        Selection.SelectionChanged += Invalidate;

        // Register all tools
        RegisterTool(new SelectTool());
        RegisterTool(new DrawTool());
        RegisterTool(new GeoTool());
        RegisterTool(new HandTool());
        RegisterTool(new TextTool());
        RegisterTool(new EraserTool());
        RegisterTool(new ArrowTool());

        // Wire ArrowShapeUtil with Store reference for binding checks
        if (ShapeUtils.Get(ShapeType.Arrow) is Shapes.ArrowShapeUtil arrowUtil)
            arrowUtil.Store = Store;

        // Activate select tool by default
        SetActiveTool(StateId.Select);
    }

    /// <summary>Register a tool and wire up its Editor reference.</summary>
    private void RegisterTool(StateNode tool)
    {
        tool.Editor = this;
        foreach (var child in tool.Children.Values)
            child.Editor = this;
        _tools[tool.Id] = tool;
    }

    /// <summary>Switch to a tool by StateId.</summary>
    public void SetActiveTool(StateId toolId)
    {
        if (!_tools.TryGetValue(toolId, out var tool))
            return;

        if (ActiveTool == tool) return;

        ActiveTool?.OnExit();
        ActiveTool = tool;
        ActiveTool.OnEnter();
        Invalidate();
    }

    /// <summary>Get a tool by StateId.</summary>
    public StateNode? GetTool(StateId toolId) =>
        _tools.TryGetValue(toolId, out var tool) ? tool : null;

    /// <summary>Notify that state changed and re-render is needed.</summary>
    public void Invalidate() => StateChanged?.Invoke();

    /// <summary>Set viewport size (called on resize).</summary>
    public void SetViewportSize(int width, int height)
    {
        Camera.ViewportSize = new SKSize(width, height);
        Invalidate();
    }


    /// <summary>Create a shape at the given world position and add it to the store.</summary>
    public TLShapeRecord? CreateShape(ShapeType shapeType, double x, double y)
    {
        var util = ShapeUtils.Get(shapeType);
        if (util is null) return null;

        var shape = util.CreateDefault(x, y);
        Store.Put(shape);
        return shape;
    }


    /// <summary>Capture current state before making changes.</summary>
    public void PushUndo() => History.PushUndo(Store);

    public void Undo()
    {
        Selection.ClearSelection();
        History.Undo(Store);
        Invalidate();
    }

    public void Redo()
    {
        Selection.ClearSelection();
        History.Redo(Store);
        Invalidate();
    }


    public void Copy()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count == 0) return;

        var snapshot = new TLStoreSnapshot { Records = shapes.Cast<TLRecord>().ToList() };
        _clipboardJson = snapshot.ToJson();
    }

    public void Paste()
    {
        if (string.IsNullOrEmpty(_clipboardJson)) return;

        var snapshot = TLStoreSnapshot.FromJson(_clipboardJson);
        if (snapshot == null) return;

        PushUndo();

        // Generate new IDs and offset positions
        var idMap = new Dictionary<string, string>();
        foreach (var record in snapshot.Records)
        {
            var oldId = record.Id;
            record.Id = Guid.NewGuid().ToString("N");
            idMap[oldId] = record.Id;

            if (record is TLShapeRecord shape)
            {
                shape.X += 20; // Offset pasted shapes
                shape.Y += 20;
            }
        }

        // Add to store
        Selection.ClearSelection();
        foreach (var record in snapshot.Records)
        {
            Store.Put(record);
            if (record is TLShapeRecord)
                Selection.Select(record.Id);
        }

        Invalidate();
    }

    public void Cut()
    {
        Copy();
        var ids = Selection.SelectedIds.ToList();
        PushUndo();
        Selection.ClearSelection();
        foreach (var id in ids)
            Store.Remove(id);
        Invalidate();
    }


    /// <summary>
    /// Create an image shape from a data URI.
    /// Called after JS interop loads the file.
    /// </summary>
    public TLShapeRecord CreateImageShape(string name, string dataUri, double width, double height, double x, double y)
    {
        PushUndo();

        var assetId = $"asset:{Guid.NewGuid().ToString("N")[..8]}";

        // Store the asset
        var asset = new TLAssetRecord
        {
            Id = assetId,
            Asset = AssetType.Image,
            Name = name,
            Src = dataUri,
            Width = width,
            Height = height,
        };
        Store.Put(asset);

        // Load image into cache
        Shapes.ImageShapeUtil.LoadImageFromDataUri(assetId, dataUri);

        // Create shape
        var shape = new TLShapeRecord
        {
            Shape = InkCanvas.Core.ShapeType.Image,
            X = x,
            Y = y,
            Width = width > 0 ? width : 200,
            Height = height > 0 ? height : 200,
            Style = new TLShapeStyle { Color = new("#ccc"), Fill = new(FillConstants.None), StrokeWidth = new(1) },
            Props = new TLImageProps { AssetId = assetId },
        };

        Store.Put(shape);
        Selection.ClearSelection();
        Selection.Select(shape.Id);
        Invalidate();

        return shape;
    }


    /// <summary>Export the entire canvas to a PNG image (raw pixel bytes).</summary>
    public byte[]? ExportToPng(int width = 1920, int height = 1080)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        if (surface == null) return null;

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Calculate scale to fit all shapes
        var allShapes = Store.GetAllShapes();
        if (allShapes.Count == 0) return null;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var shape in allShapes)
        {
            var util = ShapeUtils.Get(shape.Shape);
            if (util == null) continue;
            var bounds = util.GetBounds(shape);
            minX = Math.Min(minX, bounds.Left);
            minY = Math.Min(minY, bounds.Top);
            maxX = Math.Max(maxX, bounds.Right);
            maxY = Math.Max(maxY, bounds.Bottom);
        }

        float contentW = maxX - minX;
        float contentH = maxY - minY;
        float scale = Math.Min(width / contentW, height / contentH) * 0.9f;
        float offsetX = (width - contentW * scale) / 2 - minX * scale;
        float offsetY = (height - contentH * scale) / 2 - minY * scale;

        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale);

        // Render all shapes
        foreach (var shape in allShapes.Where(s => !s.IsHidden))
        {
            var util = ShapeUtils.Get(shape.Shape);
            if (util == null) continue;
            canvas.Save();
            util.Render(canvas, shape, scale);
            canvas.Restore();
        }

        canvas.Restore();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 95);
        return data.ToArray();
    }

    /// <summary>Export canvas as SVG string (basic implementation).</summary>
    public string ExportToSvg()
    {
        var allShapes = Store.GetAllShapes();
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var shape in allShapes)
        {
            var util = ShapeUtils.Get(shape.Shape);
            if (util == null) continue;
            var bounds = util.GetBounds(shape);
            minX = Math.Min(minX, bounds.Left);
            minY = Math.Min(minY, bounds.Top);
            maxX = Math.Max(maxX, bounds.Right);
            maxY = Math.Max(maxY, bounds.Bottom);
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{minX} {minY} {maxX - minX} {maxY - minY}\">");

        foreach (var shape in allShapes.Where(s => !s.IsHidden))
        {
            if (shape.Props is TLGeoProps geo)
            {
                var fill = shape.Style.Fill == FillConstants.None ? FillConstants.None : shape.Style.Fill;
                sb.AppendLine($"  <rect x=\"{shape.X}\" y=\"{shape.Y}\" width=\"{shape.Width}\" height=\"{shape.Height}\" fill=\"{fill}\" stroke=\"{shape.Style.Color}\" stroke-width=\"{shape.Style.StrokeWidth}\" rx=\"{shape.Style.BorderRadius}\"/>");
            }
            else if (shape.Props is TLTextProps text)
            {
                sb.AppendLine($"  <text x=\"{shape.X}\" y=\"{shape.Y + text.FontSize}\" font-size=\"{text.FontSize}\" fill=\"{shape.Style.Color}\">{System.Security.SecurityElement.Escape(text.Text)}</text>");
            }
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }


    public void Align(AlignDirection align)
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count < 2) return;

        PushUndo();

        var bounds = shapes.Select(s =>
        {
            var util = ShapeUtils.Get(s.Shape);
            return util?.GetBounds(s) ?? new SKRect((float)s.X, (float)s.Y,
                (float)(s.X + s.Width), (float)(s.Y + s.Height));
        }).ToList();

        float target = align switch
        {
            AlignDirection.Left => bounds.Min(b => b.Left),
            AlignDirection.Center => bounds.Average(b => b.MidX),
            AlignDirection.Right => bounds.Max(b => b.Right),
            AlignDirection.Top => bounds.Min(b => b.Top),
            AlignDirection.Middle => bounds.Average(b => b.MidY),
            AlignDirection.Bottom => bounds.Max(b => b.Bottom),
            _ => 0
        };

        for (int i = 0; i < shapes.Count; i++)
        {
            var shape = shapes[i];
            var b = bounds[i];

            switch (align)
            {
                case AlignDirection.Left: shape.X += target - b.Left; break;
                case AlignDirection.Center: shape.X += target - b.MidX; break;
                case AlignDirection.Right: shape.X += target - b.Right; break;
                case AlignDirection.Top: shape.Y += target - b.Top; break;
                case AlignDirection.Middle: shape.Y += target - b.MidY; break;
                case AlignDirection.Bottom: shape.Y += target - b.Bottom; break;
            }
        }

        Invalidate();
    }


    public void Distribute(DistributeDirection type)
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count < 3) return;

        PushUndo();

        if (type == DistributeDirection.Horizontal)
        {
            var sorted = shapes.OrderBy(s => s.X).ToList();
            float left = (float)sorted.First().X;
            float right = (float)(sorted.Last().X + sorted.Last().Width);
            float totalWidth = (float)sorted.Sum(s => s.Width);
            float gap = (right - left - totalWidth) / (sorted.Count - 1);

            float x = left;
            foreach (var shape in sorted)
            {
                shape.X = x;
                x += (float)shape.Width + gap;
            }
        }
        else
        {
            var sorted = shapes.OrderBy(s => s.Y).ToList();
            float top = (float)sorted.First().Y;
            float bottom = (float)(sorted.Last().Y + sorted.Last().Height);
            float totalHeight = (float)sorted.Sum(s => s.Height);
            float gap = (bottom - top - totalHeight) / (sorted.Count - 1);

            float y = top;
            foreach (var shape in sorted)
            {
                shape.Y = y;
                y += (float)shape.Height + gap;
            }
        }

        Invalidate();
    }


    public void BringToFront()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count == 0) return;
        PushUndo();

        var allShapes = Store.GetAllShapes().OrderBy(s => s.Index).ToList();
        int maxIndex = allShapes.Count;
        foreach (var shape in shapes)
            shape.Index = FractionalIndex(maxIndex++, maxIndex + 1);

        Invalidate();
    }

    public void SendToBack()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count == 0) return;
        PushUndo();

        int idx = 0;
        foreach (var shape in shapes)
            shape.Index = FractionalIndex(idx++, 100);

        Invalidate();
    }

    public void BringForward()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count == 0) return;
        PushUndo();

        var allShapes = Store.GetAllShapes().OrderBy(s => s.Index).ToList();
        foreach (var shape in shapes)
        {
            int pos = allShapes.IndexOf(shape);
            if (pos < allShapes.Count - 1)
                shape.Index = FractionalIndex(pos + 1, allShapes.Count);
        }

        Invalidate();
    }

    public void SendBackward()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count == 0) return;
        PushUndo();

        var allShapes = Store.GetAllShapes().OrderBy(s => s.Index).ToList();
        foreach (var shape in shapes)
        {
            int pos = allShapes.IndexOf(shape);
            if (pos > 0)
                shape.Index = FractionalIndex(pos - 1, allShapes.Count);
        }

        Invalidate();
    }


    public void GroupSelected()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        if (shapes.Count < 2) return;

        PushUndo();

        // Create a group shape
        var group = new TLShapeRecord
        {
            Shape = InkCanvas.Core.ShapeType.Group,
            X = shapes.Min(s => s.X),
            Y = shapes.Min(s => s.Y),
            Width = shapes.Max(s => s.X + s.Width) - shapes.Min(s => s.X),
            Height = shapes.Max(s => s.Y + s.Height) - shapes.Min(s => s.Y),
            Props = new TLGroupProps
            {
                ChildIds = shapes.Select(s => s.Id).ToList()
            }
        };

        Store.Put(group);

        // Set parent ID on children
        foreach (var shape in shapes)
            shape.ParentId = group.Id;

        Selection.ClearSelection();
        Selection.Select(group.Id);
        Invalidate();
    }

    public void UngroupSelected()
    {
        var shapes = Selection.GetSelectedShapes(Store);
        var groups = shapes.Where(s => s.Shape == InkCanvas.Core.ShapeType.Group).ToList();
        if (groups.Count == 0) return;

        PushUndo();

        Selection.ClearSelection();

        foreach (var group in shapes.Where(s => s.Shape == InkCanvas.Core.ShapeType.Group).ToList())
        {
            if (group.Props is TLGroupProps groupProps)
            {
                foreach (var childId in groupProps.ChildIds)
                {
                    var child = Store.Get<TLShapeRecord>(childId);
                    if (child != null)
                    {
                        child.ParentId = null;
                        Selection.Select(childId);
                    }
                }
            }
            Store.Remove(group.Id);
        }

        Invalidate();
    }


    /// <summary>Binding distance threshold in world units.</summary>
    public double BindingThreshold { get; set; } = 20;

    /// <summary>
    /// Try to bind an arrow endpoint to a nearby shape.
    /// Returns the binding record if successful, null otherwise.
    /// </summary>
    public TLBindingRecord? TryBindArrowEndpoint(string arrowId, ArrowEndpoint endpoint, double worldX, double worldY)
    {
        var shapes = Store.GetPageShapes();
        var point = new SKPointd(worldX, worldY);

        // Find closest shape within threshold
        TLShapeRecord? closestShape = null;
        double closestDist = BindingThreshold;

        foreach (var shape in shapes)
        {
            if (shape.Id == arrowId || shape.IsLocked || shape.IsHidden) continue;
            if (shape.Shape == InkCanvas.Core.ShapeType.Arrow) continue; // Don't bind to other arrows

            var util = ShapeUtils.Get(shape.Shape);
            if (util == null) continue;

            var bounds = util.GetBounds(shape);
            var center = new SKPointd(bounds.MidX, bounds.MidY);
            var dist = Math.Sqrt(Math.Pow(worldX - center.X, 2) + Math.Pow(worldY - center.Y, 2));

            // Check if point is near the shape
            if (dist < closestDist + Math.Max(bounds.Width, bounds.Height) / 2)
            {
                closestShape = shape;
                closestDist = dist;
            }
        }

        if (closestShape == null) return null;

        // Calculate normalized position on the shape's bounding box
        var util2 = ShapeUtils.Get(closestShape.Shape);
        if (util2 == null) return null;
        var bounds2 = util2.GetBounds(closestShape);

        var normX = (worldX - bounds2.Left) / bounds2.Width;
        var normY = (worldY - bounds2.Top) / bounds2.Height;
        normX = Math.Clamp(normX, 0, 1);
        normY = Math.Clamp(normY, 0, 1);

        // Create or update binding
        var bindingId = $"binding:{arrowId}:{endpoint.ToValue()}";
        var binding = new TLBindingRecord
        {
            Id = bindingId,
            FromShapeId = arrowId,
            ToShapeId = closestShape.Id,
            FromHandle = endpoint,
            NormalizedX = normX,
            NormalizedY = normY,
        };

        Store.Put(binding);
        return binding;
    }

    /// <summary>
    /// Remove binding for an arrow endpoint.
    /// </summary>
    public void UnbindArrowEndpoint(string arrowId, ArrowEndpoint endpoint)
    {
        var bindingId = $"binding:{arrowId}:{endpoint.ToValue()}";
        Store.Remove(bindingId);
    }

    /// <summary>
    /// Get the binding for an arrow endpoint, if any.
    /// </summary>
    public TLBindingRecord? GetArrowBinding(string arrowId, ArrowEndpoint endpoint)
    {
        var bindingId = $"binding:{arrowId}:{endpoint.ToValue()}";
        return Store.Get<TLBindingRecord>(bindingId);
    }

    /// <summary>
    /// Update arrow endpoint position based on its binding.
    /// Called when a bound shape moves or when rendering.
    /// </summary>
    public void UpdateArrowBindings(string arrowId)
    {
        var arrow = Store.Get<TLShapeRecord>(arrowId);
        if (arrow?.Props is not TLArrowProps arrowProps) return;

        // Update start binding
        var startBinding = GetArrowBinding(arrowId, ArrowEndpoint.Start);
        if (startBinding != null)
        {
            var targetShape = Store.Get<TLShapeRecord>(startBinding.ToShapeId);
            if (targetShape != null)
            {
                var util = ShapeUtils.Get(targetShape.Shape);
                if (util != null)
                {
                    var bounds = util.GetBounds(targetShape);
                    var worldX = bounds.Left + bounds.Width * startBinding.NormalizedX;
                    var worldY = bounds.Top + bounds.Height * startBinding.NormalizedY;

                    // Update arrow position and first waypoint
                    var dx = worldX - arrow.X;
                    var dy = worldY - arrow.Y;
                    arrow.X = worldX;
                    arrow.Y = worldY;

                    // Adjust all waypoints
                    for (int i = 0; i < arrowProps.Waypoints.Count; i++)
                    {
                        var pt = arrowProps.Waypoints[i];
                        arrowProps.Waypoints[i] = new SKPoint(
                            (float)(pt.X - dx),
                            (float)(pt.Y - dy));
                    }
                }
            }
            else
            {
                // Target shape was deleted — remove binding
                Store.Remove(startBinding.Id);
            }
        }

        // Update end binding
        var endBinding = GetArrowBinding(arrowId, ArrowEndpoint.End);
        if (endBinding != null)
        {
            var targetShape = Store.Get<TLShapeRecord>(endBinding.ToShapeId);
            if (targetShape != null)
            {
                var util = ShapeUtils.Get(targetShape.Shape);
                if (util != null)
                {
                    var bounds = util.GetBounds(targetShape);
                    var worldX = bounds.Left + bounds.Width * endBinding.NormalizedX;
                    var worldY = bounds.Top + bounds.Height * endBinding.NormalizedY;

                    // Update last waypoint
                    if (arrowProps.Waypoints.Count >= 2)
                    {
                        arrowProps.Waypoints[^1] = new SKPoint(
                            (float)(worldX - arrow.X),
                            (float)(worldY - arrow.Y));
                    }
                }
            }
            else
            {
                Store.Remove(endBinding.Id);
            }
        }
    }

    /// <summary>
    /// Update all arrow bindings in the store.
    /// Call this after moving shapes.
    /// </summary>
    public void UpdateAllArrowBindings()
    {
        var arrows = Store.GetAllShapes().Where(s => s.Shape == InkCanvas.Core.ShapeType.Arrow);
        foreach (var arrow in arrows)
            UpdateArrowBindings(arrow.Id);
    }

    /// <summary>
    /// Find the arrow endpoint at a given world point.
    /// Returns (arrowId, endpoint) or null.
    /// </summary>
    public (string ArrowId, ArrowEndpoint Endpoint)? HitTestArrowEndpoint(double worldX, double worldY)
    {
        var arrows = Store.GetAllShapes().Where(s => s.Shape == InkCanvas.Core.ShapeType.Arrow);
        var point = new SKPointd(worldX, worldY);
        float hitRadius = 12f / (float)Camera.Zoom;

        foreach (var arrow in arrows)
        {
            if (arrow.Props is not TLArrowProps arrowProps) continue;
            if (arrowProps.Waypoints.Count < 2) continue;

            // Check start point
            var startX = arrow.X + arrowProps.Waypoints[0].X;
            var startY = arrow.Y + arrowProps.Waypoints[0].Y;
            if (Math.Abs(worldX - startX) < hitRadius && Math.Abs(worldY - startY) < hitRadius)
                return (arrow.Id, ArrowEndpoint.Start);

            // Check end point
            var endX = arrow.X + arrowProps.Waypoints[^1].X;
            var endY = arrow.Y + arrowProps.Waypoints[^1].Y;
            if (Math.Abs(worldX - endX) < hitRadius && Math.Abs(worldY - endY) < hitRadius)
                return (arrow.Id, ArrowEndpoint.End);
        }

        return null;
    }

    private static ZIndex FractionalIndex(int pos, int total)
    {
        return new ZIndex("a" + pos.ToString("D6"));
    }


    /// <summary>Snap a value to the nearest grid increment.</summary>
    public double SnapToGrid(double value, double gridSize = 10)
    {
        return Math.Round(value / gridSize) * gridSize;
    }

    /// <summary>Toggle grid snapping on/off.</summary>
    public bool SnapEnabled { get; set; } = false;


    public void OnDoubleClick(double screenX, double screenY)
    {
        var world = Camera.ScreenToWorld(screenX, screenY);
        var shapes = Store.GetPageShapes();
        var point = new SKPointd(world.X, world.Y);

        // Find shape at point
        for (int i = shapes.Count - 1; i >= 0; i--)
        {
            var shape = shapes[i];
            if (shape.IsLocked || shape.IsHidden) continue;

            var util = ShapeUtils.Get(shape.Shape);
            if (util != null && util.HitTest(shape, point))
            {
                // If shape has text (geo with text, or text/note), switch to text tool for editing
                if (shape.Props is TLGeoProps geo)
                {
                    // Start editing geo shape text
                    Selection.ClearSelection();
                    Selection.Select(shape.Id);
                    SetActiveTool(StateId.Text);
                    var textTool = GetTool(StateId.Text) as Tools.TextTool;
                    if (textTool != null)
                    {
                        textTool.EditingShapeId = shape.Id;
                        textTool.Transition(StateId.Editing);
                    }
                    Invalidate();
                    return;
                }
                if (shape.Props is TLTextProps || shape.Props is TLNoteProps)
                {
                    Selection.ClearSelection();
                    Selection.Select(shape.Id);
                    SetActiveTool(StateId.Text);
                    var textTool = GetTool(StateId.Text) as Tools.TextTool;
                    if (textTool != null)
                    {
                        textTool.EditingShapeId = shape.Id;
                        textTool.Transition(StateId.Editing);
                    }
                    Invalidate();
                    return;
                }
            }
        }
    }


    public void OnPointerDown(double screenX, double screenY, int pointerId,
        bool shiftKey = false, bool altKey = false, bool ctrlKey = false)
    {
        IsPointerDown = true;
        PointerScreen = new SKPoint((float)screenX, (float)screenY);
        PointerWorld = Camera.ScreenToWorld(screenX, screenY);

        // Space+drag → pan mode
        if (IsSpaceHeld)
        {
            _panLastScreen = PointerScreen;
            Invalidate();
            return;
        }

        var e = new PointerEvent
        {
            ScreenX = screenX,
            ScreenY = screenY,
            WorldX = PointerWorld.X,
            WorldY = PointerWorld.Y,
            PointerId = pointerId,
            ShiftKey = shiftKey,
            AltKey = altKey,
            CtrlKey = ctrlKey,
        };

        ActiveTool.OnPointerDown(e);
    }

    public void OnPointerMove(double screenX, double screenY, int pointerId,
        bool shiftKey = false, bool altKey = false, bool ctrlKey = false)
    {
        PointerScreen = new SKPoint((float)screenX, (float)screenY);
        PointerWorld = Camera.ScreenToWorld(screenX, screenY);

        // Space+drag → panning
        if (IsSpaceHeld && IsPointerDown)
        {
            var dx = PointerScreen.X - _panLastScreen.X;
            var dy = PointerScreen.Y - _panLastScreen.Y;
            Camera.Pan(dx, dy);
            _panLastScreen = PointerScreen;
            Invalidate();
            return;
        }

        // Track hovered handle for cursor display
        if (!IsPointerDown && ActiveTool.Id == StateId.Select && Selection.Count > 0)
        {
            HoveredHandle = Selection.HitTestHandles(PointerWorld, (float)Camera.Zoom, ShapeUtils);
        }
        else if (IsPointerDown)
        {
            // Keep current handle during drag
        }
        else
        {
            HoveredHandle = SelectionManager.HandleType.None;
        }

        var e = new PointerEvent
        {
            ScreenX = screenX,
            ScreenY = screenY,
            WorldX = PointerWorld.X,
            WorldY = PointerWorld.Y,
            PointerId = pointerId,
            ShiftKey = shiftKey,
            AltKey = altKey,
            CtrlKey = ctrlKey,
        };

        ActiveTool.OnPointerMove(e);
    }

    public void OnPointerUp(double screenX, double screenY, int pointerId,
        bool shiftKey = false, bool altKey = false, bool ctrlKey = false)
    {
        IsPointerDown = false;
        PointerScreen = new SKPoint((float)screenX, (float)screenY);
        PointerWorld = Camera.ScreenToWorld(screenX, screenY);

        var e = new PointerEvent
        {
            ScreenX = screenX,
            ScreenY = screenY,
            WorldX = PointerWorld.X,
            WorldY = PointerWorld.Y,
            PointerId = pointerId,
            ShiftKey = shiftKey,
            AltKey = altKey,
            CtrlKey = ctrlKey,
        };

        ActiveTool.OnPointerUp(e);
    }

    public void OnWheel(double screenX, double screenY, double deltaY)
    {
        var screenPoint = new SKPoint((float)screenX, (float)screenY);
        var delta = deltaY > 0 ? -0.1 : 0.1;
        Camera.ZoomAt(screenPoint, delta);
        Invalidate();
    }

    public void OnKeyDown(string key, bool shiftKey = false, bool altKey = false, bool ctrlKey = false)
    {
        // Space key — temporary pan mode
        if (key == " " && !IsSpaceHeld)
        {
            IsSpaceHeld = true;
            Invalidate();
            return;
        }

        // Global keyboard shortcuts (tool-independent)
        switch (key)
        {
            // Undo / Redo
            case "z" when ctrlKey && !shiftKey:
                Undo();
                return;
            case "z" when ctrlKey && shiftKey:
                Redo();
                return;
            case "y" when ctrlKey:
                Redo();
                return;

            // Clipboard
            case "c" when ctrlKey:
                Copy();
                return;
            case "v" when ctrlKey:
                Paste();
                return;
            case "x" when ctrlKey:
                Cut();
                return;

            // Group / Ungroup
            case "g" when ctrlKey && !shiftKey:
                GroupSelected();
                return;
            case "g" when ctrlKey && shiftKey:
                UngroupSelected();
                return;

            // Tool switching (single keys, no Ctrl)
            case "v" or "V" when !ctrlKey:
                SetActiveTool(StateId.Select);
                return;
            case "d" or "D" when !ctrlKey:
                SetActiveTool(StateId.Draw);
                return;
            case "g" or "G" when !ctrlKey:
                SetActiveTool(StateId.Geo);
                return;
            case "h" or "H" when !ctrlKey:
                SetActiveTool(StateId.Hand);
                return;
            case "t" or "T" when !ctrlKey:
                SetActiveTool(StateId.Text);
                return;
            case "e" or "E" when !ctrlKey:
                SetActiveTool(StateId.Eraser);
                return;
            case "a" or "A" when !ctrlKey:
                SetActiveTool(StateId.Arrow);
                return;
        }

        // Delegate to active tool
        var e = new KeyEvent
        {
            Key = key,
            ShiftKey = shiftKey,
            AltKey = altKey,
            CtrlKey = ctrlKey,
        };

        ActiveTool.OnKeyDown(e);
    }

    public void OnKeyUp(string key)
    {
        if (key == " ")
        {
            IsSpaceHeld = false;
            Invalidate();
        }
    }


    public void Render(SKCanvas canvas, SKImageInfo info)
    {
        var w = info.Width;
        var h = info.Height;

        Camera.ViewportSize = new SKSize(w, h);
        canvas.Clear(new SKColor(0xF5, 0xF5, 0xF5));

        var matrix = Camera.GetTransformMatrix();
        canvas.Save();
        canvas.Concat(in matrix);

        DrawGrid(canvas, w, h);
        DrawOriginCrosshair(canvas);
        DrawShapes(canvas);

        // Selection indicators (world space)
        Selection.UpdateShapeCache(Store);
        Selection.RenderSelection(canvas, (float)Camera.Zoom, ShapeUtils);

        // Tool overlays (world space)
        ActiveTool.Render(canvas, (float)Camera.Zoom);

        canvas.Restore();

        // HUD (screen space)
        DrawHud(canvas, w, h);
    }

    private void DrawShapes(SKCanvas canvas)
    {
        var shapes = Store.GetPageShapes();
        var zoom = (float)Camera.Zoom;
        var viewport = Camera.GetWorldBounds();

        // Viewport culling padding (render shapes slightly outside viewport)
        double pad = 100.0 / zoom;
        double vpLeft = viewport.Left - pad;
        double vpTop = viewport.Top - pad;
        double vpRight = viewport.Right + pad;
        double vpBottom = viewport.Bottom + pad;

        foreach (var shape in shapes)
        {
            if (shape.IsHidden) continue;

            var util = ShapeUtils.Get(shape.Shape);
            if (util is null) continue;

            // Viewport culling: skip shapes outside visible area
            var bounds = util.GetBounds(shape);
            if (bounds.Right < vpLeft || bounds.Left > vpRight ||
                bounds.Bottom < vpTop || bounds.Top > vpBottom)
                continue;

            canvas.Save();

            // Use shape cache for performance (when enabled)
            if (ShapeCache.Enabled)
                ShapeCache.RenderShape(canvas, shape, util, zoom);
            else
                util.Render(canvas, shape, zoom);

            canvas.Restore();
        }
    }


    private void DrawGrid(SKCanvas canvas, int width, int height)
    {
        var bounds = Camera.GetWorldBounds();
        var gridSpacing = GetGridSpacing();

        using var gridPaint = new SKPaint
        {
            Color = new SKColor(0xDD, 0xDD, 0xDD),
            StrokeWidth = 1f / (float)Camera.Zoom,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var majorGridPaint = new SKPaint
        {
            Color = new SKColor(0xBB, 0xBB, 0xBB),
            StrokeWidth = 1.5f / (float)Camera.Zoom,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        double startX = Math.Floor(bounds.Left / gridSpacing) * gridSpacing;
        for (double x = startX; x <= bounds.Right; x += gridSpacing)
        {
            var paint = Math.Abs(x) < 0.001 ? majorGridPaint :
                        (Math.Abs(x % (gridSpacing * 5)) < 0.001 ? majorGridPaint : gridPaint);
            var screenX = (float)((x - Camera.Position.X) * Camera.Zoom);
            canvas.DrawLine(screenX, 0, screenX, height, paint);
        }

        double startY = Math.Floor(bounds.Top / gridSpacing) * gridSpacing;
        for (double y = startY; y <= bounds.Bottom; y += gridSpacing)
        {
            var paint = Math.Abs(y) < 0.001 ? majorGridPaint :
                        (Math.Abs(y % (gridSpacing * 5)) < 0.001 ? majorGridPaint : gridPaint);
            var screenY = (float)((y - Camera.Position.Y) * Camera.Zoom);
            canvas.DrawLine(0, screenY, width, screenY, paint);
        }
    }

    private double GetGridSpacing()
    {
        var baseSpacing = 50.0;
        var zoom = Camera.Zoom;
        if (zoom < 0.2) return baseSpacing * 8;
        if (zoom < 0.5) return baseSpacing * 4;
        if (zoom < 1.0) return baseSpacing * 2;
        if (zoom > 4.0) return baseSpacing / 2;
        return baseSpacing;
    }

    private void DrawOriginCrosshair(SKCanvas canvas)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(0xFF, 0x6B, 0x6B),
            StrokeWidth = 2f / (float)Camera.Zoom,
            IsAntialias = true
        };
        float size = 20f / (float)Camera.Zoom;
        canvas.DrawLine(-size, 0, size, 0, paint);
        canvas.DrawLine(0, -size, 0, size, paint);
    }

    private void DrawHud(SKCanvas canvas, int width, int height)
    {
        using var paint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
        using var font = new SKFont { Size = 12 };

        canvas.DrawText($"{Camera.Zoom:P0}", width - 60, height - 12, SKTextAlign.Left, font, paint);
        canvas.DrawText($"({Camera.Position.X:F0}, {Camera.Position.Y:F0})", 8, height - 12, SKTextAlign.Left, font, paint);

        var shapeCount = Store.GetAllShapes().Count;
        var selectedCount = Selection.Count;
        var countText = selectedCount > 0
            ? $"{shapeCount} shapes ({selectedCount} selected)"
            : $"{shapeCount} shapes";
        canvas.DrawText(countText, 8, height - 28, SKTextAlign.Left, font, paint);

        canvas.DrawText($"Tool: {ActiveTool.Id.ToValue()}", 8, height - 44, SKTextAlign.Left, font, paint);
    }
}
