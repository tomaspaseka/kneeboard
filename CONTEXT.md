# Domain Language

Terms used across Kneeboard's code, tests and architecture notes. If a name changes here, it
changes in the code too — that's the point of the file.

## Document

A `.kneeboard` file: one mission's worth of material. JSON; see
[`arch/kneeboard-format.md`](arch/kneeboard-format.md). Exactly one document is open at a time.

## Section

A named tab within a document — "Mission Datacard", "Airfields Map". Has an id, a label, and
exactly one content source.

## Content source

Where a section's material comes from, and the only thing that varies between kinds of section.
Two kinds today: a **pdf** (one file) and an **images** folder (many files, ordered by filename).
Modelled as `ContentSource`, with `PdfSource` and `ImageFolderSource` subtypes.

## Page

One image the pilot looks at, shown on its own — never two side by side, and never cropped to fill
the screen. Pages are ordered within a section and addressed by index. A page **is encoded image
bytes** — deliberately not a file path. Nothing above the section source knows whether a page ever
existed on disk.

## Section source

The module that turns a section's content source into that section's ordered pages
(`ISectionSource`). It is the only place that knows how each kind of content source becomes pages,
so callers learn one interface instead of one mechanism per kind.

Pages are produced eagerly when the document opens and held for as long as it stays open. Failure
to read a source throws — there is no partially-loaded document.

Watch the collision: a **section source** is the module; a **content source** is the field on a
section that it consumes. Both end in "source" and they are not the same thing.

## Rasterizer

The platform-specific part of paging a pdf (`IPdfRasterizer`): `Windows.Data.Pdf` on Windows,
`Android.Graphics.Pdf` on Android. It sits *behind* the section source rather than beside it —
nothing outside the section source knows that rasterizing happens at all.

A page that fails to rasterize is reported as failed rather than dropped, and the section source
substitutes a placeholder image. Page count therefore always matches the original document: a
pilot sees that a page is missing instead of silently flying with a short one.

## Page navigation zone

The part of the screen where a tap pages the section: the outer fifth of the page on each side, plus
any empty space beyond it out to the screen edge. The middle three fifths of the page is not a
navigation zone — that is left to zoom and pan. Measured against the page as currently drawn, so
zooming moves the zones with it. Not "tap zone" and not "nav zone".
