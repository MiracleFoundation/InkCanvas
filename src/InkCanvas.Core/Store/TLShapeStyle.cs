namespace InkCanvas.Core.Store;

/// <summary>Visual style for shapes.</summary>
public class TLShapeStyle
{
    /// <summary>Stroke color as hex (#RGB or #RRGGBB).</summary>
    public HexColor Color { get; set; } = new("#1e1e1e");

    /// <summary>Fill color as hex, or FillConstants.None for transparent.</summary>
    public HexColor Fill { get; set; } = new(FillConstants.None);

    /// <summary>Stroke width in world units. Must be >= 0 (0 = no stroke).</summary>
    public NonNegativeDouble StrokeWidth { get; set; } = new(2.0);

    /// <summary>Opacity 0.0–1.0.</summary>
    public UnitInterval Opacity { get; set; } = UnitInterval.One;

    /// <summary>Dash pattern for strokes.</summary>
    public DashPattern DashPattern { get; set; } = DashPattern.Solid;

    /// <summary>Border radius for rectangles (0 = sharp). Must be >= 0.</summary>
    public NonNegativeDouble BorderRadius { get; set; } = NonNegativeDouble.Zero;

    /// <summary>Font size for text shapes. Must be > 0.</summary>
    public PositiveDouble FontSize { get; set; } = new(16);
}
