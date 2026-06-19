# Tldraw.Blazor

Infinite canvas whiteboard SDK for Blazor, ported from [tldraw](https://tldraw.dev/) — powered by SkiaSharp.

- 🎨 **8 shape types** — Geo, Draw, Text, Note, Frame, Line, Arrow, Image
- 🛠️ **8 tools** — Select, Draw, Geo, Hand, Text, Arrow, Eraser + Space-pan
- ✏️ **Full interaction** — Select, move, resize, rotate, draw, erase, text editing
- 🔗 **Arrow bindings** — Endpoints snap to shapes, auto-update on move
- ↩️ **Undo/Redo** — Snapshot-based, 50 steps
- 📋 **Clipboard** — Copy/Cut/Paste
- 📐 **Alignment** — Align, distribute, z-order, grouping
- 🎯 **Style panel** — Color, fill, stroke, opacity, dash, radius, font
- 💾 **Persistence** — Auto-save to LocalStorage, export/import JSON/PNG/SVG
- 📱 **Touch support** — Pinch-to-zoom, two-finger pan
- 🌙 **Dark mode** — Full theme support
- ⚡ **Performance** — Viewport culling, shape caching

## Install

```bash
git clone https://github.com/your-repo/Tldraw.Blazor.git
cd Tldraw.Blazor
dotnet build
```

## Run

```bash
dotnet run --project src/Tldraw.Blazor.App --urls "http://localhost:5000"
```

Open `http://localhost:5000` in your browser.

## Usage

### As a Component

```razor
@using Tldraw.Blazor.Skia
@using Tldraw.Blazor.Core.Editor

<TldrawCanvas Editor="_editor" />

@code {
    private Editor _editor = new();

    protected override void OnInitialized()
    {
        // Create shapes programmatically
        _editor.CreateShape("geo", 100, 100);
        var rect = _editor.Store.GetAllShapes().Last();
        rect.Width = 200;
        rect.Height = 120;
        rect.Style.Fill = "#E3F2FD";
        rect.Style.Color = "#1565C0";
        if (rect.Props is TLGeoProps props)
        {
            props.GeoType = "rectangle";
            props.Text = "Hello!";
        }
    }
}
```

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `V` | Select tool |
| `D` | Draw tool |
| `G` | Geo tool |
| `H` | Hand tool |
| `T` | Text tool |
| `A` | Arrow tool |
| `E` | Eraser tool |
| `Space+Drag` | Pan (any tool) |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+C/V/X` | Copy/Paste/Cut |
| `Ctrl+G` | Group selected |
| `Ctrl+Shift+G` | Ungroup selected |
| `Ctrl+A` | Select all |
| `Del` | Delete selected |
| `Escape` | Deselect / Cancel |
| `Scroll` | Zoom |
| `Shift+Drag` | Constrain proportions |
| `Alt+Drag` | Resize from center |

## Architecture

```
Tldraw.Blazor.Core/           ← Pure C#, no UI dependencies
├── Editor/
│   ├── Camera.cs              ← Pan/Zoom/Transform
│   ├── Editor.cs              ← Central engine
│   ├── SelectionManager.cs    ← Selection + handles
│   ├── HistoryManager.cs      ← Undo/redo
│   └── ShapeCache.cs          ← Performance caching
├── Shapes/                    ← 8 shape renderers
├── Store/                     ← Reactive record store
└── Tools/                     ← 8 tool implementations

Tldraw.Blazor.Skia/           ← SkiaSharp Blazor component
└── TldrawCanvas.razor         ← Canvas with touch support

Tldraw.Blazor.App/            ← Demo app
├── Components/                ← UI panels
└── Pages/Home.razor           ← Full demo
```

## Tech Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10.0 |
| UI | Blazor WebAssembly | 10.0 |
| Rendering | SkiaSharp | 3.119.4 |
| Canvas | SkiaSharp.Views.Blazor | 3.119.4 |

## License

[MIT](LICENSE.txt)
