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

## Binder

The material of an open document as the pilot has it in front of them: every section's pages, the page
they are on in each, and how each is framed. Immutable (`Binder`) — turning, selecting and framing each
return a new binder, and one that changes nothing returns itself. It is the only place that knows where
the pilot is, so nothing can hold two answers at once.

## Framing

How far into a page the pilot is zoomed, and which point of the page sits at the centre of the screen.
Held per **section**, not per page: switch tabs and come back and the framing is restored, but turning a
page clears it back to the fit. Expressed relative to the page rather than in screen pixels, so it means
the same thing at any window size and survives a resize untouched. Not "zoom level" and not "viewport" —
both name only half of it.

## Page navigation zone

The part of the screen where a tap pages the section: the outer fifth of the page on each side, plus
any empty space beyond it out to the screen edge. The middle three fifths of the page is not a
navigation zone — that is left to zoom and pan. Measured in the page's own coordinates, so the zones are
attached to the page and not to the screen: zooming carries them out of view with the rest of the page,
and paging while zoomed needs the pilot panned to an edge. Not "tap zone" and not "nav zone".
