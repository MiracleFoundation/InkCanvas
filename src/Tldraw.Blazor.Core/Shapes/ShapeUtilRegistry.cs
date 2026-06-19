namespace Tldraw.Blazor.Core.Shapes;

/// <summary>
/// Registry mapping shape type strings to their ShapeUtil implementations.
/// </summary>
public class ShapeUtilRegistry
{
    private readonly Dictionary<string, ShapeUtil> _utils = new();

    public ShapeUtilRegistry()
    {
        // Register built-in shape utils
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
    public ShapeUtil? Get(string shapeType) =>
        _utils.TryGetValue(shapeType, out var util) ? util : null;

    /// <summary>All registered shape types.</summary>
    public IReadOnlyCollection<string> RegisteredTypes => _utils.Keys;
}
