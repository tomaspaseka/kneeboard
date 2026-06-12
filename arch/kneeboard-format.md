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

Paths are relative to the `.kneeboard` file's directory; `DocumentService` resolves them to absolute at load time. Image folders load `.png .jpg .jpeg .bmp .gif .webp` files sorted by filename.
