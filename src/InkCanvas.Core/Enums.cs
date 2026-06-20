using System.Text.Json.Serialization;

namespace InkCanvas.Core;

/// <summary>Store record type discriminator.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecordType
{
    [JsonPropertyName("shape")]
    Shape,
    [JsonPropertyName("page")]
    Page,
    [JsonPropertyName("binding")]
    Binding,
    [JsonPropertyName("asset")]
    Asset,
}

/// <summary>Asset type discriminator.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetType
{
    Image,
    Video,
}

/// <summary>Shape type discriminator.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShapeType
{
    Geo,
    Draw,
    Text,
    Note,
    Frame,
    Line,
    Arrow,
    Image,
    Group,
}

/// <summary>Tool identifier.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ToolId
{
    Select,
    Draw,
    Geo,
    Hand,
    Text,
    Arrow,
    Eraser,
}

/// <summary>Dash pattern for strokes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DashPattern
{
    Solid,
    Dashed,
    Dotted,
}

/// <summary>Text alignment.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextAlign
{
    Left,
    Center,
    Right,
}

/// <summary>Font weight.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FontWeight
{
    Normal,
    Bold,
}

/// <summary>Arrow endpoint side.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArrowEndpoint
{
    Start,
    End,
}

/// <summary>Arrow routing style.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArrowStyle
{
    Arrow,
    Elbow,
}

/// <summary>Geo shape variant.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GeoVariant
{
    Rectangle,
    Ellipse,
    Diamond,
    Star,
    Hexagon,
    Triangle,
    Cloud,
}

/// <summary>Alignment direction.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlignDirection
{
    Left,
    Center,
    Right,
    Top,
    Middle,
    Bottom,
}

/// <summary>Distribution direction.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DistributeDirection
{
    Horizontal,
    Vertical,
}

/// <summary>State machine state identifiers (tools + child states).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StateId
{
    // Tools (root states)
    Select,
    Draw,
    Geo,
    Hand,
    Text,
    Arrow,
    Eraser,

    // Child states
    Idle,
    Pointing,
    Dragging,
    Brush,
    Resizing,
    Rotating,
    ArrowEditing,
    Drawing,
    Creating,
    Editing,
    Erasing,
    Panning,
}

/// <summary>Static fill value for transparent/no-fill.</summary>
public static class FillConstants
{
    public const string None = "none";
}

/// <summary>Extension methods for enums to get string values.</summary>
public static class EnumExtensions
{
    public static string ToValue(this RecordType type) => type switch
    {
        RecordType.Shape => "shape",
        RecordType.Page => "page",
        RecordType.Binding => "binding",
        RecordType.Asset => "asset",
        _ => type.ToString().ToLowerInvariant(),
    };

    public static string ToValue(this AssetType type) => type switch
    {
        AssetType.Image => "image",
        AssetType.Video => "video",
        _ => type.ToString().ToLowerInvariant(),
    };

    public static string ToValue(this StateId id) => id switch
    {
        StateId.Select => "select",
        StateId.Draw => "draw",
        StateId.Geo => "geo",
        StateId.Hand => "hand",
        StateId.Text => "text",
        StateId.Arrow => "arrow",
        StateId.Eraser => "eraser",
        StateId.Idle => "idle",
        StateId.Pointing => "pointing",
        StateId.Dragging => "dragging",
        StateId.Brush => "brush",
        StateId.Resizing => "resizing",
        StateId.Rotating => "rotating",
        StateId.ArrowEditing => "arrowEditing",
        StateId.Drawing => "drawing",
        StateId.Creating => "creating",
        StateId.Editing => "editing",
        StateId.Erasing => "erasing",
        StateId.Panning => "panning",
        _ => id.ToString().ToLowerInvariant(),
    };

    public static string ToValue(this ShapeType type) => type switch
    {
        ShapeType.Geo => "geo",
        ShapeType.Draw => "draw",
        ShapeType.Text => "text",
        ShapeType.Note => "note",
        ShapeType.Frame => "frame",
        ShapeType.Line => "line",
        ShapeType.Arrow => "arrow",
        ShapeType.Image => "image",
        ShapeType.Group => "group",
        _ => type.ToString().ToLowerInvariant(),
    };

    public static string ToValue(this ToolId id) => id switch
    {
        ToolId.Select => "select",
        ToolId.Draw => "draw",
        ToolId.Geo => "geo",
        ToolId.Hand => "hand",
        ToolId.Text => "text",
        ToolId.Arrow => "arrow",
        ToolId.Eraser => "eraser",
        _ => id.ToString().ToLowerInvariant(),
    };

    public static string ToValue(this DashPattern pattern) => pattern switch
    {
        DashPattern.Solid => "solid",
        DashPattern.Dashed => "dashed",
        DashPattern.Dotted => "dotted",
        _ => "solid",
    };

    public static string ToValue(this TextAlign align) => align switch
    {
        TextAlign.Left => "left",
        TextAlign.Center => "center",
        TextAlign.Right => "right",
        _ => "left",
    };

    public static string ToValue(this FontWeight weight) => weight switch
    {
        FontWeight.Normal => "normal",
        FontWeight.Bold => "bold",
        _ => "normal",
    };

    public static string ToValue(this ArrowEndpoint endpoint) => endpoint switch
    {
        ArrowEndpoint.Start => "start",
        ArrowEndpoint.End => "end",
        _ => "start",
    };

    public static string ToValue(this ArrowStyle style) => style switch
    {
        ArrowStyle.Arrow => "arrow",
        ArrowStyle.Elbow => "elbow",
        _ => "arrow",
    };

    public static string ToValue(this GeoVariant variant) => variant switch
    {
        GeoVariant.Rectangle => "rectangle",
        GeoVariant.Ellipse => "ellipse",
        GeoVariant.Diamond => "diamond",
        GeoVariant.Star => "star",
        GeoVariant.Hexagon => "hexagon",
        GeoVariant.Triangle => "triangle",
        GeoVariant.Cloud => "cloud",
        _ => "rectangle",
    };

    public static string ToValue(this AlignDirection dir) => dir switch
    {
        AlignDirection.Left => "left",
        AlignDirection.Center => "center",
        AlignDirection.Right => "right",
        AlignDirection.Top => "top",
        AlignDirection.Middle => "middle",
        AlignDirection.Bottom => "bottom",
        _ => "left",
    };

    public static string ToValue(this DistributeDirection dir) => dir switch
    {
        DistributeDirection.Horizontal => "horizontal",
        DistributeDirection.Vertical => "vertical",
        _ => "horizontal",
    };
}
