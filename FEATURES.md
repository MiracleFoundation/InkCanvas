# tldraw Blazor SkiaSharp — Feature Status

> Port of [tldraw](https://tldraw.dev/) to Blazor WASM + SkiaSharp (pure C#)
> **All features implemented** ✅

---

## ✅ All Features Implemented

### Foundation
- Blazor WASM + SkiaSharp 3.119.4 (.NET 10)
- Camera pan/zoom (drag, scroll, zoom controls, Space+drag)
- Grid rendering (adaptive by zoom)
- HUD overlay (zoom, position, shape count, tool name)

### Store & Shapes
- Reactive store with events
- **8 shape types**: Geo, Draw, Text, Note, Frame, Line, Arrow, Image
- Shape style: color, fill, stroke, opacity, dash, radius, font size
- JSON polymorphic serialization
- Bindings for arrow endpoints

### Tools (8)
| Tool | Key | Notes |
|------|-----|-------|
| Select | V | Select/move/resize/rotate/brush/arrow-edit |
| Draw | D | Freehand with smooth bezier curves |
| Geo | G | Rectangle/ellipse/diamond/star/hex/tri |
| Hand | H | Pan canvas |
| Text | T | Click to place, type to edit, double-click existing |
| Arrow | A | Drag to create, auto-bind endpoints |
| Eraser | E | Click/drag to delete |
| — | Space | Temporary pan from any tool |

### Selection & Transform
- Click/Shift-click/rubber-band select
- 8 resize handles (Shift=constrain, Alt=center, Escape=cancel)
- Rotation handle (drag to rotate, Shift=snap 15°)
- Multi-shape resize
- Arrow endpoint editing (click→drag→rebind)

### Arrow System
- Arrow creation with arrowhead
- Arrow bindings (snap to shapes, green=bound, auto-update on move)
- Endpoint editing (click→drag→rebind)
- Visual feedback (green dot=bound, blue=unbound)

### Layout & Composition
- Alignment (left/center/right/top/middle/bottom)
- Distribution (horizontal/vertical even spacing)
- Z-ordering (front/back/forward/backward)
- Grouping (Ctrl+G group, Ctrl+Shift+G ungroup)
- Snapping (grid snap method)

### Undo / Redo / Clipboard
- Undo (Ctrl+Z, snapshot-based, 50 steps)
- Redo (Ctrl+Y)
- Copy/Cut/Paste (Ctrl+C/V/X)

### Canvas Features
- Viewport culling (skip off-screen shapes)
- **Shape caching** (render to SKBitmap, version-based invalidation)
- **Touch support** (pinch-to-zoom, two-finger pan)
- Draw smoothing (quadratic bezier)
- Minimap (overview with viewport indicator)

### Data & Persistence
- Auto-save to LocalStorage (every 5s)
- Export JSON → clipboard
- Import JSON ← clipboard
- Export PNG → download file
- Export SVG → basic generation
- **Image loading** (file picker → data URI → SKBitmap cache)

### UI Components
- Toolbar (12 tools + zoom + export/import + image)
- Style panel (color, fill, stroke, opacity, dash, radius, font)
- Context menu (clipboard, z-order, align, group)
- Zoom controls (+/−, fit, 1:1)
- Keyboard shortcuts help (⌨️ toggle)
- Pages panel (multi-page management)
- Dark mode (🌙 toggle, full theme)
- Minimap (overview with viewport)

### Image System
- `ImageShapeUtil` with placeholder rendering
- `ImageShapeUtil.LoadImage()` / `LoadImageFromDataUri()`
- JS interop file picker (`image-interop.js`)
- Image asset storage (`TLAssetRecord`)
- Bitmap cache per asset

### Shape Caching
- `ShapeCache` class with version-based invalidation
- Per-shape SKBitmap caching
- Auto-invalidate on store changes
- Configurable max cache size (200 shapes)
- Fallback to direct rendering on failure

---

## Architecture

```
Tldraw.Blazor.Core/
├── Editor/
│   ├── Camera.cs           — Pan/Zoom/Transform
│   ├── Editor.cs           — Central engine, tools, clipboard, undo
│   ├── SelectionManager.cs — Selection + handles + hit-testing
│   ├── HistoryManager.cs   — Snapshot-based undo/redo
│   └── ShapeCache.cs       — SKBitmap caching for performance
├── Shapes/
│   ├── ShapeUtil.cs, ShapeUtilRegistry.cs
│   ├── GeoShapeUtil.cs, DrawShapeUtil.cs, TextShapeUtil.cs
│   ├── NoteShapeUtil.cs, FrameShapeUtil.cs, LineShapeUtil.cs
│   ├── ArrowShapeUtil.cs (with binding support)
│   └── ImageShapeUtil.cs (with bitmap cache)
├── Store/
│   ├── TLRecord.cs (Shape/Page/Binding/Asset)
│   └── TLStore.cs (reactive store + snapshots)
└── Tools/
    ├── StateNode.cs, PointerEvent.cs
    ├── SelectTool.cs (6 states incl. ArrowEditing)
    ├── DrawTool.cs, GeoTool.cs, HandTool.cs
    ├── TextTool.cs, EraserTool.cs, ArrowTool.cs

Tldraw.Blazor.Skia/
└── TldrawCanvas.razor + .razor.cs (touch gestures, cursor logic)

Tldraw.Blazor.App/
├── wwwroot/image-interop.js — JS interop for file loading
├── Components/
│   ├── StylePanel, ContextMenu, Minimap
│   ├── PagesPanel, DarkModeToggle
└── Pages/Home.razor — toolbar + canvas + all components
```

## Tech Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10.0 |
| UI Framework | Blazor WebAssembly | 10.0 |
| 2D Rendering | SkiaSharp | 3.119.4 |
| Canvas Bridge | SkiaSharp.Views.Blazor | 3.119.4 |
| Serialization | System.Text.Json | (framework) |
| JS Interop | image-interop.js | (minimal, file loading only) |
