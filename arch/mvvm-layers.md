# MVVM Layers

| Layer | Where | Role |
|---|---|---|
| Models | `Models/` | Plain data: `KneeboardDocument`, `KneeboardSection`, `ContentSource` (polymorphic via custom `JsonConverter`) |
| Services | `Services/` | `IDocumentService` — file picker + JSON load + path resolution + validation; `IPdfService` — PDF→ImageSource rendering (platform-specific) |
| ViewModels | `ViewModels/` | `WelcomeViewModel`, `KneeboardViewModel` — CommunityToolkit.Mvvm `[ObservableProperty]` |
| Views | `Views/` | `WelcomePage`, `KneeboardPage` — XAML + code-behind |
