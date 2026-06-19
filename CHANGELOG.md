# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2026-06-19

### Added

#### Foundation
- Blazor WASM + SkiaSharp 3.119.4 (.NET 10)
- Camera with pan, zoom, screen/world coordinate conversion
- Adaptive grid rendering
- HUD overlay (zoom, position, shape count, tool name)

#### Store & Shapes
- Reactive `TLStore` with change events
- 8 shape types: Geo, Draw, Text, Note, Frame, Line, Arrow, Image
- Shape style system (color, fill, stroke, opacity, dash, radius, font)
- JSON polymorphic serialization (`TLStoreSnapshot`)
- Binding records for arrow endpoints

#### Tools (8)
- **Select** — Select, move, resize, rotate, rubber-band, arrow endpoint editing
- **Draw** — Freehand drawing with smooth bezier curves
- **Geo** — Create rectangle, ellipse, diamond, star, hexagon, triangle by drag
- **Hand** — Canvas panning
- **Text** — Click to place text, type to edit, double-click existing shapes
- **Arrow** — Drag to create arrows with auto-binding endpoints
- **Eraser** — Click/drag to delete shapes
- **Space+Drag** — Temporary pan from any tool

#### Selection & Transform
- Click, Shift+click, rubber-band selection
- 8 resize handles with Shift (constrain) and Alt (center) modifiers
- Rotation handle with Shift (snap 15°)
- Multi-shape resize
- Arrow endpoint editing with rebind

#### Arrow System
- Arrow creation with arrowhead
- Arrow bindings (green dot = bound, blue = unbound)
- Auto-update arrow positions when bound shapes move
- Endpoint editing (click→drag→rebind)

#### Layout & Composition
- Alignment (left/center/right/top/middle/bottom)
- Distribution (horizontal/vertical even spacing)
- Z-ordering (front/back/forward/backward)
- Grouping (Ctrl+G / Ctrl+Shift+G)

#### Undo / Redo / Clipboard
- Snapshot-based undo (50 steps max)
- Redo (Ctrl+Y / Ctrl+Shift+Z)
- Copy/Cut/Paste (Ctrl+C/V/X)

#### Canvas Features
- Viewport culling
- Shape caching (SKBitmap, version-based invalidation)
- Touch support (pinch-to-zoom, two-finger pan)
- Draw shape smoothing (quadratic bezier)
- Minimap with viewport indicator

#### Data & Persistence
- Auto-save to LocalStorage (every 5 seconds)
- Export/Import JSON (clipboard)
- Export PNG (file download)
- Export SVG (basic generation)
- Image loading (file picker → data URI → SKBitmap cache)

#### UI Components
- Toolbar (12 tools + zoom + export/import + image)
- Style panel (color, fill, stroke, opacity, dash, radius, font)
- Context menu (clipboard, z-order, alignment, grouping)
- Zoom controls (+/−, fit, 1:1)
- Keyboard shortcuts help panel
- Pages panel (multi-page management)
- Dark mode toggle with full theme
- Minimap overview

#### Documentation
- README.md with usage examples
- CLAUDE.md for Claude Code instructions
- AGENTS.md for agent instructions
- TEST-CHECKLIST.md with 70+ manual test cases
- CONTRIBUTING.md with development guidelines
- CHANGELOG.md (this file)
- FEATURES.md with complete feature status
- LICENSE.txt (MIT)
