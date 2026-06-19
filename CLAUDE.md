# Tldraw.Blazor — Claude Code Instructions

## Project Overview

Port of tldraw (React/TypeScript infinite canvas SDK) to Blazor WASM + SkiaSharp, pure C# with minimal JS interop.

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run --project src/Tldraw.Blazor.App --urls "http://localhost:5000"

# Clean
dotnet clean
```

## Project Structure

```
Tldraw.Blazor.Core/        — Pure C# engine (no UI deps)
Tldraw.Blazor.Skia/        — SkiaSharp Blazor component
Tldraw.Blazor.App/         — Demo Blazor WASM app
```

## Key Patterns

### State Machine (Tools)
All tools use hierarchical `StateNode` state machines:
```
SelectTool → IdleState → PointingState → DraggingState
           → BrushState
           → ResizingState
           → RotatingState
           → ArrowEditingState
```

### Reactive Store
`TLStore` emits `Changed` events. All shapes/pages/bindings/assets are `TLRecord` subclasses with JSON polymorphic serialization.

### Shape Utils
Each shape type has a `ShapeUtil` subclass: `CreateDefault()`, `Render()`, `GetBounds()`, `HitTest()`.

### Rendering Pipeline
```
Editor.Render() → DrawGrid() → DrawShapes() → SelectionManager.RenderSelection() → ActiveTool.Render() → DrawHud()
```

## Code Conventions

- **No magic strings** — use constants or enums where possible
- **Nullable enabled** — all projects have `<Nullable>enable</Nullable>`
- **SkiaSharp 3.x** — use `SKFont` instead of deprecated `SKPaint.TextSize`
- **Editor namespace conflict** — use `using Editor = Tldraw.Blazor.Core.Editor.Editor;` in Tools namespace
- **SKPointd/SKRectd** — custom double-precision types in `Camera.cs` (SkiaSharp only has float)

## Testing

Run the demo app and test:
1. All 8 tools work (V/D/G/H/T/A/E)
2. Select → resize → rotate → move
3. Arrow bindings (green dots when bound)
4. Undo/redo (Ctrl+Z/Y)
5. Copy/paste (Ctrl+C/V)
6. Style panel changes
7. Context menu actions
8. Dark mode toggle
9. Touch gestures (pinch-to-zoom)
10. Auto-save/load from localStorage

## Common Issues

| Issue | Fix |
|-------|-----|
| `CS0118: 'Editor' is a namespace but used like a type` | Add `using Editor = Tldraw.Blazor.Core.Editor.Editor;` |
| `CS0246: 'SKPointd' could not be found` | Add `using Tldraw.Blazor.Core.Editor;` |
| `CS0618: 'SKPaint.TextSize' is obsolete` | Use `SKFont.Size` instead |
| Native assets warning | Expected for non-AOT WASM builds, can ignore |
