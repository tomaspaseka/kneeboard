# Platform-Specific Code

`IPdfService` has two implementations under `Platforms/`:
- **Windows:** `Windows.Data.Pdf` → renders pages to PNG files in a temp directory → returns `ImageSource` list
- **Android:** `Android.Graphics.Pdf.PdfRenderer`

Registration is done with `#if WINDOWS / #elif ANDROID` in `MauiProgram.cs`.

`Platforms/Windows/Program.cs` provides a custom `Main` that calls `Bootstrap.Initialize()` before `Application.Start()`. This is required because `WindowsAppSdkDeploymentManagerInitialize=false` and `DISABLE_XAML_GENERATED_MAIN` are set to prevent the auto-generated initializer from firing when `Kneeboard.dll` is loaded by the test runner.
