using System.Text.Json.Serialization;

namespace Tldraw.Blazor.Core.Store;

/// <summary>
/// Base class for all store records. Every entity in the editor
/// (shapes, pages, bindings, assets) derives from this.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TLShapeRecord), "shape")]
[JsonDerivedType(typeof(TLPageRecord), "page")]
[JsonDerivedType(typeof(TLBindingRecord), "binding")]
[JsonDerivedType(typeof(TLAssetRecord), "asset")]
public abstract class TLRecord
{
    /// <summary>Unique identifier (GUID string).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Record type discriminator: "shape", "page", "binding", "asset".</summary>
    public abstract string Type { get; }
}

/// <summary>
/// A shape on the canvas. Holds position, size, rotation,
/// shape-type-specific props, and visual style.
/// </summary>
public class TLShapeRecord : TLRecord
{
    public override string Type => "shape";

    /// <summary>Shape sub-type: "geo", "draw", "text", "arrow", "line", "note", "frame", "image", "group".</summary>
    public string ShapeType { get; set; } = "";

    /// <summary>X position in world space.</summary>
    public double X { get; set; }

    /// <summary>Y position in world space.</summary>
    public double Y { get; set; }

    /// <summary>Width in world units.</summary>
    public double Width { get; set; }

    /// <summary>Height in world units.</summary>
    public double Height { get; set; }

    /// <summary>Rotation in radians.</summary>
    public double Rotation { get; set; }

    /// <summary>Parent ID (for grouped shapes or frames).</summary>
    public string? ParentId { get; set; }

    /// <summary>Z-ordering key (fractional indexing).</summary>
    public string Index { get; set; } = "a0";

    /// <summary>Whether the shape is locked.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Whether the shape is hidden.</summary>
    public bool IsHidden { get; set; }

    /// <summary>Visual style.</summary>
    public TLShapeStyle Style { get; set; } = new();

    /// <summary>Shape-type-specific properties (polymorphic).</summary>
    public TLShapeProps Props { get; set; } = new TLGeoProps();
}

/// <summary>Visual style for shapes.</summary>
public class TLShapeStyle
{
    /// <summary>Stroke color as hex, e.g. "#000000".</summary>
    public string Color { get; set; } = "#1e1e1e";

    /// <summary>Fill color as hex, or "none" for transparent.</summary>
    public string Fill { get; set; } = "none";

    /// <summary>Stroke width in world units.</summary>
    public double StrokeWidth { get; set; } = 2.0;

    /// <summary>Opacity 0.0–1.0.</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>Dash pattern: "solid", "dashed", "dotted".</summary>
    public string DashPattern { get; set; } = "solid";

    /// <summary>Border radius for rectangles (0 = sharp).</summary>
    public double BorderRadius { get; set; }

    /// <summary>Font size for text shapes.</summary>
    public double FontSize { get; set; } = 16;
}

// ── Shape-specific props ────────────────────────────────────

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
    /// <summary>Geometry type: "rectangle", "ellipse", "diamond", "star", "hexagon", "triangle", "cloud".</summary>
    public string GeoType { get; set; } = "rectangle";

    /// <summary>Optional text content inside the shape.</summary>
    public string? Text { get; set; }
}

/// <summary>Props for freehand drawing.</summary>
public class TLDrawProps : TLShapeProps
{
    /// <summary>Segments as flat array: [x1,y1,x2,y2,...]. Each segment is a stroke.</summary>
    public List<List<double>> Segments { get; set; } = new();

    /// <summary>Whether the stroke is complete.</summary>
    public bool IsComplete { get; set; }
}

/// <summary>Props for text shapes.</summary>
public class TLTextProps : TLShapeProps
{
    public string Text { get; set; } = "";
    public double FontSize { get; set; } = 24;
    public string TextAlign { get; set; } = "left"; // "left", "center", "right"
    public string FontWeight { get; set; } = "normal"; // "normal", "bold"
}

/// <summary>Props for arrow shapes.</summary>
public class TLArrowProps : TLShapeProps
{
    public string? StartBindingId { get; set; }
    public string? EndBindingId { get; set; }
    public List<List<double>> Waypoints { get; set; } = new(); // [[x,y], ...]
    public string ArrowType { get; set; } = "arrow"; // "arrow", "elbow"
}

/// <summary>Props for line shapes.</summary>
public class TLLineProps : TLShapeProps
{
    public List<List<double>> Points { get; set; } = new(); // [[x,y], ...]
}

/// <summary>Props for sticky note shapes.</summary>
public class TLNoteProps : TLShapeProps
{
    public string Text { get; set; } = "";
    public string NoteColor { get; set; } = "#FFEB3B"; // yellow
}

/// <summary>Props for frame shapes.</summary>
public class TLFrameProps : TLShapeProps
{
    public string Name { get; set; } = "Frame";
}

/// <summary>Props for image shapes.</summary>
public class TLImageProps : TLShapeProps
{
    public string AssetId { get; set; } = "";
}

/// <summary>Props for group shapes.</summary>
public class TLGroupProps : TLShapeProps
{
    public List<string> ChildIds { get; set; } = new();
}

// ── Other record types ──────────────────────────────────────

/// <summary>A page in the document.</summary>
public class TLPageRecord : TLRecord
{
    public override string Type => "page";
    public string Name { get; set; } = "Page 1";
    public int Index { get; set; }
}

/// <summary>A binding between shapes (e.g. arrow endpoints).</summary>
public class TLBindingRecord : TLRecord
{
    public override string Type => "binding";
    public string FromShapeId { get; set; } = "";
    public string ToShapeId { get; set; } = "";
    public string FromHandle { get; set; } = ""; // "start", "end"
    public double NormalizedX { get; set; }
    public double NormalizedY { get; set; }
}

/// <summary>An asset (image, video, etc.).</summary>
public class TLAssetRecord : TLRecord
{
    public override string Type => "asset";
    public string AssetType { get; set; } = ""; // "image", "video"
    public string Name { get; set; } = "";
    public string? Src { get; set; } // URL or data URI
    public double Width { get; set; }
    public double Height { get; set; }
}
