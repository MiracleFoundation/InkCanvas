# Tldraw.Blazor — Agent Instructions

## Project Overview

Port of tldraw (infinite canvas whiteboard) to Blazor WASM + SkiaSharp. Pure C# engine with minimal JS interop (only image file loading).

## Architecture

### Layers
1. **Tldraw.Blazor.Core** — Pure C# engine (no UI dependencies)
2. **Tldraw.Blazor.Skia** — SkiaSharp Blazor rendering component
3. **Tldraw.Blazor.App** — Demo application with UI components

### Key Classes

| Class | Location | Purpose |
|-------|----------|---------|
| `Editor` | Core/Editor/Editor.cs | Central engine, owns Store, Camera, tools |
| `Camera` | Core/Editor/Camera.cs | Pan, zoom, coordinate transforms |
| `TLStore` | Core/Store/TLStore.cs | Reactive record store with events |
| `TLShapeRecord` | Core/Store/TLRecord.cs | Shape data (position, size, style, props) |
| `StateNode` | Core/Tools/StateNode.cs | Hierarchical state machine base |
| `ShapeUtil` | Core/Shapes/ShapeUtil.cs | Shape rendering/hit-test base |
| `SelectionManager` | Core/Editor/SelectionManager.cs | Selection tracking + handle rendering |
| `HistoryManager` | Core/Editor/HistoryManager.cs | Snapshot-based undo/redo |
| `ShapeCache` | Core/Editor/ShapeCache.cs | SKBitmap caching for performance |

### Data Flow
```
User Input → TldrawCanvas → Editor → ActiveTool → StateNode
                                                      ↓
                                               Store.Put/Remove
                                                      ↓
                                               Store.Changed event
                                                      ↓
                                               Editor.Invalidate()
                                                      ↓
                                               Render() → SKCanvas
```

## Common Tasks

### Adding a Shape Type
1. Add `TLXxxProps : TLShapeProps` to `TLRecord.cs`
2. Create `XxxShapeUtil : ShapeUtil` in `Shapes/`
3. Register in `ShapeUtilRegistry` constructor
4. Add to `TLShapeProps` JSON discriminator

### Adding a Tool
1. Create `XxxTool : StateNode` in `Tools/`
2. Add child states
3. Register in `Editor` constructor
4. Add keyboard shortcut in `Editor.OnKeyDown()`
5. Add cursor in `TldrawCanvas.CursorStyle`

### Modifying Selection Behavior
- Selection logic is in `SelectTool.cs`
- Handle hit-testing is in `SelectionManager.HitTestHandles()`
- Resize logic is in `SelectTool.ResizingState`
- Rotation logic is in `SelectTool.RotatingState`

## Code Conventions

- **Nullable enabled** — use `?` for nullable types
- **SkiaSharp 3.x** — use `SKFont` not `SKPaint.TextSize`
- **Editor namespace** — use `using Editor = Tldraw.Blazor.Core.Editor.Editor;` in Tools
- **Double precision** — use `SKPointd`/`SKRectd` for world coordinates
- **State machine** — all tools use `StateNode` with `Transition()`

## Build & Test

```bash
dotnet build
dotnet run --project src/Tldraw.Blazor.App --urls "http://localhost:5000"
```

See [TEST-CHECKLIST.md](TEST-CHECKLIST.md) for manual test cases.
