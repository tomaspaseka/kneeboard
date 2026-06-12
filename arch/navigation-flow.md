# Navigation Flow

`WelcomePage` → user opens file → `DocumentService.PickAndLoadAsync()` → on success `Shell.GoToAsync("kneeboard", document)` → `KneeboardViewModel` renders all PDF sections via `PdfService` behind a loading overlay → `CarouselView` populated, overlay dismissed. The 📂 icon on `KneeboardPage` re-runs the same flow and replaces the current document in place.
