# Tldraw.Blazor — Manual Test Checklist

## 1. Tools

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 1.1 | Select tool | Press V, click shape | Shape selected with blue outline + handles | |
| 1.2 | Draw tool | Press D, drag on canvas | Freehand stroke created | |
| 1.3 | Geo tool | Press G, drag on canvas | Rectangle created | |
| 1.4 | Geo variants | Click Rect/Ellipse/Diamond/Star/Hex/Tri buttons | Correct shape created | |
| 1.5 | Hand tool | Press H, drag | Canvas pans | |
| 1.6 | Text tool | Press T, click, type text | Text shape created | |
| 1.7 | Arrow tool | Press A, drag | Arrow with arrowhead created | |
| 1.8 | Eraser tool | Press E, click shape | Shape deleted | |
| 1.9 | Space+pan | Hold Space, drag | Canvas pans from any tool | |
| 1.10 | Tool highlight | Click toolbar buttons | Active tool highlighted | |

## 2. Selection & Transform

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 2.1 | Click select | Click on shape | Blue outline + 8 handles + rotation handle | |
| 2.2 | Shift multi-select | Shift+click multiple shapes | All selected | |
| 2.3 | Rubber-band | Drag on empty space | Blue rectangle, shapes inside selected | |
| 2.4 | Move shape | Drag selected shape | Shape moves | |
| 2.5 | Resize corner | Drag corner handle | Shape resizes | |
| 2.6 | Resize edge | Drag edge handle | Shape resizes in one direction | |
| 2.7 | Shift constrain | Shift+drag corner | Proportional resize | |
| 2.8 | Alt center | Alt+drag handle | Resize from center | |
| 2.9 | Rotate | Drag rotation handle | Shape rotates | |
| 2.10 | Shift snap rotate | Shift+rotate | Snaps to 15° increments | |
| 2.11 | Multi-resize | Select 2+ shapes, drag handle | All resize together | |
| 2.12 | Delete | Press Del | Selected shapes deleted | |
| 2.13 | Select all | Ctrl+A | All shapes selected | |
| 2.14 | Deselect | Escape | Selection cleared | |

## 3. Arrow Bindings

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 3.1 | Create bound arrow | Drag arrow from near shape to another | Green dots at endpoints | |
| 3.2 | Move bound shape | Select shape, drag | Arrow follows | |
| 3.3 | Edit endpoint | Click arrow endpoint, drag | Endpoint moves, rebinds if near shape | |
| 3.4 | Unbind | Drag endpoint away from shape | Dot turns blue | |
| 3.5 | Rebind | Drag endpoint near another shape | Dot turns green | |

## 4. Text Editing

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 4.1 | Create text | Press T, click, type | Text appears | |
| 4.2 | Edit existing | Double-click text shape | Cursor appears, can edit | |
| 4.3 | Edit geo text | Double-click rectangle with text | Text editing mode | |
| 4.4 | Finish editing | Press Escape or Enter | Editing ends | |
| 4.5 | Delete text | Backspace during edit | Characters deleted | |

## 5. Undo / Redo / Clipboard

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 5.1 | Undo | Ctrl+Z after move | Shape returns to original position | |
| 5.2 | Redo | Ctrl+Y after undo | Move re-applied | |
| 5.3 | Copy | Ctrl+C with selection | Shapes copied | |
| 5.4 | Paste | Ctrl+V | Shapes pasted with offset | |
| 5.5 | Cut | Ctrl+X | Shapes cut (removed + copied) | |

## 6. Layout & Composition

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 6.1 | Align left | Right-click → Align Left | All shapes align to leftmost | |
| 6.2 | Align center | Right-click → Align Center | Shapes centered horizontally | |
| 6.3 | Distribute H | Right-click → Distribute H | Even horizontal spacing | |
| 6.4 | Bring to front | Right-click → Bring to Front | Shape on top | |
| 6.5 | Send to back | Right-click → Send to Back | Shape on bottom | |
| 6.6 | Group | Ctrl+G with 2+ selected | Shapes grouped | |
| 6.7 | Ungroup | Ctrl+Shift+G with group selected | Group dissolved | |

## 7. Style Panel

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 7.1 | Panel appears | Select shape | Style panel visible | |
| 7.2 | Stroke color | Click color swatch | Shape border color changes | |
| 7.3 | Fill color | Click fill swatch | Shape fill changes | |
| 7.4 | Stroke width | Drag slider | Border thickness changes | |
| 7.5 | Opacity | Drag slider | Shape transparency changes | |
| 7.6 | Dash pattern | Click dashed/dotted | Border style changes | |
| 7.7 | Border radius | Drag slider (geo only) | Corner radius changes | |
| 7.8 | Font size | Drag slider (text only) | Text size changes | |

## 8. Canvas Features

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 8.1 | Zoom in | Scroll up | Canvas zooms in at cursor | |
| 8.2 | Zoom out | Scroll down | Canvas zooms out at cursor | |
| 8.3 | Zoom buttons | Click +/− buttons | Zoom changes | |
| 8.4 | Fit | Click ⊞ Fit | Reset to fit all | |
| 8.5 | 1:1 | Click 1:1 | Reset to 100% | |
| 8.6 | Grid | Zoom in/out | Grid spacing adapts | |
| 8.7 | HUD | Look at bottom-left | Zoom %, position, shape count shown | |

## 9. Persistence

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 9.1 | Auto-save | Create shapes, wait 5s | Data saved to localStorage | |
| 9.2 | Auto-load | Refresh page | Shapes restored | |
| 9.3 | Export JSON | Click 💾 JSON | JSON copied to clipboard | |
| 9.4 | Import JSON | Click 📂 Import | Shapes loaded from clipboard | |
| 9.5 | Export PNG | Click 🖼️ PNG | PNG file downloaded | |
| 9.6 | Add image | Click 🖼️+ Image | File picker opens, image added | |

## 10. UI Components

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 10.1 | Context menu | Right-click canvas | Menu appears with options | |
| 10.2 | Minimap | Look at bottom-left | Overview with viewport indicator | |
| 10.3 | Pages panel | Look at bottom-right | Page list with add button | |
| 10.4 | Dark mode | Click 🌙 | Dark theme applied | |
| 10.5 | Shortcuts help | Click ⌨️ | Shortcuts panel toggles | |

## 11. Touch (Mobile/Tablet)

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 11.1 | Single finger | Touch and drag | Pan/select works | |
| 11.2 | Pinch zoom | Two fingers pinch | Zoom in/out | |
| 11.3 | Two finger pan | Two fingers drag | Canvas pans | |

## 12. Performance

| # | Test Case | Steps | Expected | OK? |
|---|-----------|-------|----------|-----|
| 12.1 | Many shapes | Create 50+ shapes | Smooth rendering | |
| 12.2 | Viewport culling | Zoom in on area | Only visible shapes rendered | |
| 12.3 | Shape cache | Move/resize shapes | Cache invalidated correctly | |
