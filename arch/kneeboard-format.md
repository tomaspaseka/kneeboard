# `.kneeboard` File Format

```json
{
  "title": "EPKK 2026-06-10",
  "sections": [
    { "id": "datacard", "label": "Mission Datacard", "source": { "type": "pdf", "path": "relative/or/absolute.pdf" } },
    { "id": "airfields", "label": "Airfields Map", "source": { "type": "images", "folder": "relative/or/absolute/folder" } }
  ]
}
```

Paths are relative to the `.kneeboard` file's directory; `DocumentService` resolves them to absolute at load time.

`SectionSource` turns each section's content source into that section's pages. It is the single place
that knows how each source kind becomes pages — image folders take `.png .jpg .jpeg .bmp .gif .webp`
(case-insensitive) sorted by filename; pdfs go to the platform rasterizer. Those rules are covered by
`Kneeboard.Tests/Services/SectionSourceTests.cs` rather than described only here.
