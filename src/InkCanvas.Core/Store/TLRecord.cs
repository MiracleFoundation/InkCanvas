using System.Text.Json.Serialization;
using InkCanvas.Core;

namespace InkCanvas.Core.Store;

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

    /// <summary>Record type discriminator.</summary>
    public abstract RecordType RecordType { get; }
}

/// <summary>
/// A shape on the canvas. Holds position, size, rotation,
/// shape-type-specific props, and visual style.
/// </summary>
public class TLShapeRecord : TLRecord
{
    public override RecordType RecordType => RecordType.Shape;

    /// <summary>Shape sub-type.</summary>
    public ShapeType Shape { get; set; }

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

    /// <summary>Z-ordering key (fractional indexing). Supports comparison and insertion.</summary>
    public ZIndex Index { get; set; } = new("a0");

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
    public override RecordType RecordType => RecordType.Page;
    public string Name { get; set; } = "Page 1";
    public int Index { get; set; }
}

/// <summary>A binding between shapes (e.g. arrow endpoints).</summary>
public class TLBindingRecord : TLRecord
{
    public override RecordType RecordType => RecordType.Binding;
    public string FromShapeId { get; set; } = string.Empty;
    public string ToShapeId { get; set; } = string.Empty;
    public ArrowEndpoint FromHandle { get; set; } = ArrowEndpoint.Start;
    public double NormalizedX { get; set; }
    public double NormalizedY { get; set; }
}

/// <summary>An asset (image, video, etc.).</summary>
public class TLAssetRecord : TLRecord
{
    public override RecordType RecordType => RecordType.Asset;
    public AssetType Asset { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Src { get; set; } 
    public double Width { get; set; }
    public double Height { get; set; }
}
