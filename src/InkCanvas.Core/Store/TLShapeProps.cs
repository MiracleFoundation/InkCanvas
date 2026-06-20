using System.Text.Json.Serialization;
using SkiaSharp;

namespace InkCanvas.Core.Store;

/// <summary>Base class for shape-type-specific properties.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TLGeoProps), "geo")]
[JsonDerivedType(typeof(TLDrawProps), "draw")]
[JsonDerivedType(typeof(TLTextProps), "text")]
[JsonDerivedType(typeof(TLArrowProps), "arrow")]
[JsonDerivedType(typeof(TLLineProps), "line")]
[JsonDerivedType(typeof(TLNoteProps), "note")]
[JsonDerivedType(typeof(TLFrameProps), "frame")]
[JsonDerivedType(typeof(TLImageProps), "image")]
[JsonDerivedType(typeof(TLGroupProps), "group")]
public abstract class TLShapeProps { }

/// <summary>Props for geometric shapes (rectangle, ellipse, diamond, star, hexagon, triangle).</summary>
public class TLGeoProps : TLShapeProps
{
    /// <summary>Geometry type.</summary>
    public GeoVariant GeoType { get; set; } = GeoVariant.Rectangle;

    /// <summary>Optional text content inside the shape.</summary>
    public string? Text { get; set; }
}

/// <summary>Props for freehand drawing.</summary>
public class TLDrawProps : TLShapeProps
{
    /// <summary>Each segment is a list of points forming one stroke.</summary>
    [JsonConverter(typeof(SKPointListJsonConverter))]
    public List<List<SKPoint>> Segments { get; set; } = new();

    /// <summary>Whether the stroke is complete.</summary>
    public bool IsComplete { get; set; }
}

/// <summary>Props for text shapes.</summary>
public class TLTextProps : TLShapeProps
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Font size. Must be > 0.</summary>
    public PositiveDouble FontSize { get; set; } = new(24);

    public TextAlign TextAlign { get; set; } = TextAlign.Left;
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;
}

/// <summary>Props for arrow shapes.</summary>
public class TLArrowProps : TLShapeProps
{
    public string? StartBindingId { get; set; }
    public string? EndBindingId { get; set; }

    /// <summary>Arrow waypoints as points relative to shape origin.</summary>
    [JsonConverter(typeof(SKPointListJsonConverter))]
    public List<SKPoint> Waypoints { get; set; } = new();

    public ArrowStyle ArrowType { get; set; } = ArrowStyle.Arrow;
}

/// <summary>Props for line shapes.</summary>
public class TLLineProps : TLShapeProps
{
    /// <summary>Line vertices as points relative to shape origin.</summary>
    [JsonConverter(typeof(SKPointListJsonConverter))]
    public List<SKPoint> Points { get; set; } = new();
}

/// <summary>Props for sticky note shapes.</summary>
public class TLNoteProps : TLShapeProps
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Note background color. Must be valid hex (#RGB or #RRGGBB).</summary>
    public HexColor NoteColor { get; set; } = new("#FFEB3B");
}

/// <summary>Props for frame shapes.</summary>
public class TLFrameProps : TLShapeProps
{
    public string Name { get; set; } = "Frame";
}

/// <summary>Props for image shapes.</summary>
public class TLImageProps : TLShapeProps
{
    public string AssetId { get; set; } = string.Empty;
}

/// <summary>Props for group shapes.</summary>
public class TLGroupProps : TLShapeProps
{
    public List<string> ChildIds { get; set; } = new();
}
