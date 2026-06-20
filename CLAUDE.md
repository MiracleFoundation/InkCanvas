# InkCanvas — Claude Code Instructions

## Project Overview

Infinite canvas whiteboard SDK for Blazor, powered by SkiaSharp. Pure C# engine with minimal JS interop.

## Build & Run

```bash
# Build
dotnet build

# Run (docs + demo)
dotnet run --project docs/InkCanvas.Docs --urls "http://localhost:5000"

# Clean
dotnet clean
```

## Project Structure

```
InkCanvas.Core/              — Pure C# engine (no UI deps)
InkCanvas.Canvas/            — Blazor canvas component (TldrawCanvas)
docs/InkCanvas.Docs/         — Documentation site + Live Demo
```

## Key Patterns

### State Machine (Tools)
All tools use hierarchical `StateNode` state machines with `StateId` enum:
```
SelectTool → IdleState → PointingState → DraggingState
           → BrushState → ResizingState → RotatingState → ArrowEditingState
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

- **No magic strings** — use enums (`StateId`, `ShapeType`, `ToolId`, etc.)
- **Strong typed values** — use wrapper types (`PositiveDouble`, `HexColor`, `ZIndex`, etc.)
- **Nullable enabled** — all projects have `<Nullable>enable</Nullable>`
- **SkiaSharp 3.x** — use `SKFont` instead of deprecated `SKPaint.TextSize`

## Common Issues

| Issue | Fix |
|-------|-----|
| `CS0118: 'Editor' is a namespace` | Use fully qualified `Tldraw.Core.Editor.Editor` |
| `CS0246: 'SKPointd' not found` | Add `using Tldraw.Core.Editor;` |
| `CS0618: 'SKPaint.TextSize' obsolete` | Use `SKFont.Size` instead |
