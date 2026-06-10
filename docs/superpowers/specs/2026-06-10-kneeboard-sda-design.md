# Kneeboard — Single Document App Design
Date: 2026-06-10

## Overview

Kneeboard is a single-document viewer for pilots. On launch, the user selects a `.kneeboard` JSON file that describes two content sections. Each section is displayed as a tab; pages within a section are navigated by horizontal swipe.

Primary platform: Windows 11 tablet. Secondary: Android.

---

## Document Format

A `.kneeboard` file is a JSON document. It contains a title and an ordered list of sections. Each section points to a content source — either a single PDF file or a folder of images.

```json
{
  "title": "EPKK 2026-06-10",
  "sections": [
    {
      "id": "datacard",
      "label": "Mission Datacard",
      "source": {
        "type": "pdf",
        "path": "C:\\Flights\\datacard.pdf"
      }
    },
    {
      "id": "airfields",
      "label": "Airfields Map",
      "source": {
        "type": "images",
        "folder": "C:\\Flights\\airfields"
      }
    }
  ]
}
```

**Path resolution:** paths may be absolute or relative. Relative paths are resolved relative to the `.kneeboard` file's own directory.

**Image folder loading:** all files with extensions `.png`, `.jpg`, `.jpeg`, `.bmp`, `.gif`, `.webp` are loaded from the folder, sorted by filename ascending. Files with other extensions are ignored.

**First iteration sections:** exactly two — Mission Datacard and Airfields Map. The schema is not limited to two, but the UI is designed around the two-tab case.

---

## Model Classes

```
KneeboardDocument
  string Title
  List<KneeboardSection> Sections

KneeboardSection
  string Id
  string Label
  ContentSource Source

ContentSource (abstract)
  PdfSource : ContentSource
    string Path          // absolute, resolved at load time
  ImageFolderSource : ContentSource
    string Folder        // absolute, resolved at load time
```

Deserialized from JSON using `System.Text.Json` with a custom `JsonConverter` for the `ContentSource` discriminated union (keyed on `"type"`).

---

## Architecture

Five units with clear boundaries:

### DocumentService
Responsible for the full open-file flow:
1. Shows the OS file picker filtered to `*.kneeboard`
2. Reads and deserializes the JSON
3. Resolves relative paths to absolute using the file's directory as base
4. Validates that every referenced PDF file and image folder exists on disk
5. Returns `KneeboardDocument` on success, or a typed error

No UI knowledge. Injectable via the DI container.

### PdfService
Responsible for converting a PDF file into an ordered list of `ImageSource`, one per page. Renders all pages eagerly when called — no lazy loading.

Platform implementations:
- **Windows:** `Windows.Data.Pdf` namespace (`PdfDocument`, `PdfPage.RenderToStreamAsync`)
- **Android:** `Android.Graphics.Pdf.PdfRenderer`

Each platform implementation lives in `Platforms/Windows/` and `Platforms/Android/` respectively, registered in `MauiProgram.cs` via `#if` conditional or partial class.

### WelcomePage + WelcomeViewModel
The launch screen. Displays the app name and a single "Open Kneeboard File…" button. On successful load, navigates to `KneeboardPage` passing the `KneeboardDocument` as a Shell navigation parameter. Error messages displayed inline (no blocking dialogs).

### KneeboardPage + KneeboardViewModel
The main viewer. Layout:

- **Tab bar (top):** one tab per section. Active tab highlighted. Document title and 📂 re-open icon on the right side.
- **Content area:** a `CarouselView` with `HorizontalSnapPointsType.MandatorySingle`. Each item is a full-screen `Image` displaying a rendered page. `ItemsSource` is bound to the active section's page list.
- **Page indicator (bottom):** a row of dots showing current position within the active section. Always visible.
- **Loading overlay:** covers the `CarouselView` while `PdfService` renders pages. Dismissed when all sections are ready.

Tab switching sets `SelectedSectionIndex`, swaps `CarouselView.ItemsSource`, and resets to page 0.

The 📂 icon triggers the same `DocumentService.PickAndLoadAsync()` flow, replacing the current document in place.

---

## Navigation & Data Flow

```
App start
  → WelcomePage

User taps "Open Kneeboard File…"
  → DocumentService.PickAndLoadAsync()
      → OS file picker
      → Deserialize JSON
      → Resolve paths
      → Validate files exist
      → Return KneeboardDocument

On success
  → Shell.GoToAsync("kneeboard", document)
  → KneeboardViewModel receives document
  → PdfService.RenderAllPagesAsync() for each PDF section
  → Loading overlay shown during render
  → CarouselView populated, overlay dismissed
  → First tab selected by default

User taps inactive tab
  → SelectedSectionIndex updated
  → CarouselView.ItemsSource swapped
  → CarouselView scrolls to page 0

User taps 📂 icon
  → Same flow as "Open Kneeboard File…"
  → Current document replaced in place
```

---

## Error Handling

All errors are surfaced to the user with actionable messages. No silent failures.

| Scenario | Behaviour |
|---|---|
| File picker cancelled | No-op. Stay on Welcome screen. |
| JSON parse failure | Inline error on Welcome: "Could not read kneeboard file. Check that it's valid JSON." |
| Missing PDF file | Load aborted. Error names the section and path: "Mission Datacard: file not found at …". Stay on Welcome screen. |
| Missing image folder | Load aborted. Same format as above. Stay on Welcome screen. |
| PDF page render failure | That page replaced with an error placeholder image. Rest of document loads normally. Non-fatal. |
| Empty image folder | Section gets zero pages. `CarouselView` shows "No pages" placeholder. Non-fatal. |

---

## Out of Scope (this iteration)

- More than two sections
- Zoom / pinch-to-zoom on pages
- Landscape / portrait rotation handling
- OneDrive or remote file sources
- Creating or editing `.kneeboard` files within the app
- iOS and macOS targets
