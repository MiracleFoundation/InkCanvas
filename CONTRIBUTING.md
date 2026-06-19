# Contributing to Tldraw.Blazor

Thank you for your interest in contributing!

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch
4. Make your changes
5. Submit a pull request

## Development Setup

```bash
# Prerequisites
# - .NET 10 SDK
# - Visual Studio 2022 or VS Code with C# extension

# Clone
git clone https://github.com/your-username/Tldraw.Blazor.git
cd Tldraw.Blazor

# Build
dotnet build

# Run demo
dotnet run --project src/Tldraw.Blazor.App --urls "http://localhost:5000"
```

## Project Structure

```
Tldraw.Blazor.Core/        — Core engine (pure C#, no UI deps)
├── Editor/                 — Camera, Editor, Selection, History
├── Shapes/                 — Shape utils (render, bounds, hit-test)
├── Store/                  — Reactive record store
└── Tools/                  — Tool state machines

Tldraw.Blazor.Skia/        — SkiaSharp Blazor component
└── TldrawCanvas.razor      — Canvas with touch support

Tldraw.Blazor.App/         — Demo app
├── Components/             — UI panels (Style, Context, Minimap, etc.)
└── Pages/Home.razor        — Full demo page
```

## Code Guidelines

### Naming
- Classes: PascalCase (`ShapeUtil`, `Camera`)
- Methods: PascalCase (`GetBounds`, `HitTest`)
- Properties: PascalCase (`IsSelected`, `CurrentPageId`)
- Fields: _camelCase (`_records`, `_cache`)
- Interfaces: I-prefix (`IDisposable`)

### Patterns
- Use `StateNode` for tool states (hierarchical state machine)
- Use `TLRecord` subclasses for store data
- Use `ShapeUtil` subclasses for shape rendering
- Use `SKFont` instead of deprecated `SKPaint.TextSize` (SkiaSharp 3.x)

### Nullable
All projects have `<Nullable>enable</Nullable>`. Use `?` for nullable types.

### Namespace Conflict
In `Tldraw.Blazor.Core.Tools`, use:
```csharp
using Editor = Tldraw.Blazor.Core.Editor.Editor;
```

## Adding a New Shape Type

1. Create `TLXxxProps : TLShapeProps` in `TLRecord.cs`
2. Create `XxxShapeUtil : ShapeUtil` in `Shapes/`
3. Register in `ShapeUtilRegistry` constructor
4. Implement `CreateDefault()`, `Render()`, `GetBounds()`, `HitTest()`

## Adding a New Tool

1. Create `XxxTool : StateNode` in `Tools/`
2. Add child states (`IdleState`, `ActiveState`, etc.)
3. Register in `Editor` constructor: `RegisterTool(new XxxTool())`
4. Add keyboard shortcut in `Editor.OnKeyDown()`
5. Add cursor in `TldrawCanvas.CursorStyle`
6. Add toolbar button in `Home.razor`

## Testing

See [TEST-CHECKLIST.md](TEST-CHECKLIST.md) for manual test cases.

```bash
# Build and run
dotnet build
dotnet run --project src/Tldraw.Blazor.App

# Test all tools, selection, undo/redo, clipboard, etc.
```

## Pull Request Guidelines

- Keep PRs focused on one feature/fix
- Update FEATURES.md if adding features
- Update TEST-CHECKLIST.md if adding test cases
- Test all affected functionality before submitting

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE.txt).
