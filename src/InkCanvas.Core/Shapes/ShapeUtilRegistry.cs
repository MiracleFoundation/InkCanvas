namespace InkCanvas.Core.Shapes;

/// <summary>
/// Registry mapping ShapeType enum to ShapeUtil implementations.
/// </summary>
public class ShapeUtilRegistry
{
    private readonly Dictionary<ShapeType, ShapeUtil> _utils = new();

    public ShapeUtilRegistry()
    {
        Register(new GeoShapeUtil());
        Register(new DrawShapeUtil());
        Register(new TextShapeUtil());
        Register(new NoteShapeUtil());
        Register(new FrameShapeUtil());
        Register(new LineShapeUtil());
        Register(new ArrowShapeUtil());
        Register(new ImageShapeUtil());
    }

    /// <summary>Register a shape util.</summary>
    public void Register(ShapeUtil util)
    {
        _utils[util.ShapeType] = util;
    }

    /// <summary>Get the shape util for a given type, or null.</summary>
    public ShapeUtil? Get(ShapeType shapeType) =>
        _utils.TryGetValue(shapeType, out var util) ? util : null;

    /// <summary>All registered shape types.</summary>
    public IReadOnlyCollection<ShapeType> RegisteredTypes => _utils.Keys;
}
