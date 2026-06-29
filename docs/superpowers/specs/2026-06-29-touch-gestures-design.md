# Touch Gesture Design — Zoomable Page Viewer

**Date:** 2026-06-29  
**Platform:** Windows touchscreen only  
**Status:** Approved

---

## Overview

Replace the existing `CarouselView`-based page viewer with a custom `ZoomablePageView` control backed by a native WinUI `ScrollViewer`. This resolves the smoothness issues of the previous MAUI gesture recognizer approach by delegating pinch-to-zoom and pan entirely to the OS.

---

## Gesture Map

| Gesture | Action |
|---|---|
| Pinch (2 fingers) | Zoom in / out (1× – 3×) |
| Double-tap | Reset zoom to fit page (animated) |
| 1-finger drag | Pan (ScrollViewer handles; no-op at 1×) |
| Tap left edge zone (20% of width) | Previous page |
| Tap right edge zone (20% of width) | Next page |

**Pan / edge-tap conflict:** WinUI's `Tapped` event only fires when the finger has not moved significantly. A drag gesture starting in the edge zone reaches the `ScrollViewer` naturally — no manual threshold logic needed.

---

## Component Structure

### New control: `ZoomablePageView`

Shared project (`Controls/ZoomablePageView.cs`):

```
ZoomablePageView : View
  Properties:
    ImageSource      (string)   — file path of current page image
    PreviousPageCommand (ICommand)
    NextPageCommand     (ICommand)
```

Windows handler (`Platforms/Windows/ZoomablePageViewHandler.cs`):

```
WinUI Grid (full size)
  ├─ ScrollViewer
  │    ZoomMode = Enabled
  │    HorizontalScrollMode = Enabled
  │    VerticalScrollMode = Enabled
  │    MinZoomFactor = 1.0f
  │    MaxZoomFactor = 3.0f
  │    ScrollBarVisibility = Hidden (both axes)
  │    └─ Image (Stretch=Uniform)
  └─ Grid overlay (3 columns: 20% | 60% | 20%)
       ├─ Rectangle col 0 — Tapped → PreviousPageCommand
       └─ Rectangle col 2 — Tapped → NextPageCommand
```

`DoubleTapped` on the `ScrollViewer` calls:
```csharp
scrollViewer.ChangeView(null, null, 1.0f, disableAnimation: false);
```

`ImageSource` changes trigger `MapImageSource`: create `BitmapImage` from path, assign to inner `Image.Source`, reset zoom to 1× centered.

---

## ViewModel Changes

`KneeboardViewModel` additions:

```csharp
// Computed property — current page's image file path
public string CurrentPageImagePath =>
    CurrentPages.Count > 0 ? CurrentPages[CurrentPageIndex] : string.Empty;

[RelayCommand]
void PreviousPage() => CurrentPageIndex = Math.Max(0, CurrentPageIndex - 1);

[RelayCommand]
void NextPage() => CurrentPageIndex = Math.Min(CurrentPages.Count - 1, CurrentPageIndex + 1);
```

`CurrentPageIndex` setter must also call `OnPropertyChanged(nameof(CurrentPageImagePath))`.

---

## KneeboardPage.xaml Change

Replace `CarouselView` with:

```xml
<controls:ZoomablePageView
    ImageSource="{Binding CurrentPageImagePath}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}" />
```

The existing page indicator dots remain bound to `CurrentPageDots` — no change needed there.

---

## Edge Cases

| Scenario | Behaviour |
|---|---|
| Single-page document | Edge taps are no-ops (clamped by Math.Max/Min) |
| Section switch | `CurrentPageIndex` resets to 0 → `ImageSource` changes → zoom resets to 1× |
| Portrait / landscape pages | `Stretch=Uniform` always fits page at 1× |
| Drag starting in edge zone | `Tapped` never fires for moving fingers; pan reaches ScrollViewer |
| At 1× zoom, finger drag | ScrollViewer content fits viewport — no scroll occurs |

---

## Out of Scope

- Non-Windows platforms (stub throws `PlatformNotSupportedException`)
- Zoom level persistence across page changes (always resets to 1×)
- Programmatic zoom controls (no +/- buttons)
