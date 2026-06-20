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
    public string ShapeType { get; set; } = string.Empty;

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
    public ArrowEndpoint FromHandle { get; set; } = ArrowEndpoint.Start;
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
