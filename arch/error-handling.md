# Error Handling

Errors surface inline (no blocking dialogs). `DocumentLoadResult` carries success / cancelled / error states. Missing files abort the load and name the section and path. PDF page render failures are non-fatal (placeholder image per page). Empty image folders show a "No pages" placeholder.
