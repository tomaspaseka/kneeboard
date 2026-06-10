# Kneeboard Single Document App — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Kneeboard single-document viewer: JSON document picker on launch, two-tab viewer (Mission Datacard + Airfields Map), horizontal swipe between PDF pages / images, dot page indicator.

**Architecture:** MVVM with CommunityToolkit.Mvvm. `DocumentService` owns file picking and validation. `PdfService` renders PDF pages eagerly to `ImageSource` using platform-native APIs (Windows.Data.Pdf / Android.PdfRenderer). `KneeboardPage` hosts a `CarouselView` with horizontal snap driven by `KneeboardViewModel`. Navigation abstracted behind `INavigationService` for testability.

**Tech Stack:** .NET 10, C#, .NET MAUI 10, CommunityToolkit.Maui 14.2.0, CommunityToolkit.Mvvm 8.4.2, xUnit 2.9.3

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Kneeboard.Tests/Kneeboard.Tests.csproj` | Create | xUnit test project targeting Windows |
| `Kneeboard.Tests/Models/DocumentDeserializationTests.cs` | Create | JSON round-trip tests |
| `Kneeboard.Tests/Services/DocumentServiceTests.cs` | Create | Path resolution + validation tests |
| `Kneeboard.Tests/ViewModels/WelcomeViewModelTests.cs` | Create | Open-file command tests |
| `Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs` | Create | Section selection + loading tests |
| `Kneeboard/Models/KneeboardDocument.cs` | Create | `KneeboardDocument`, `KneeboardSection` |
| `Kneeboard/Models/ContentSource.cs` | Create | `ContentSource` (abstract), `PdfSource`, `ImageFolderSource`, JSON converter |
| `Kneeboard/Services/IDocumentService.cs` | Create | Interface + `DocumentLoadResult` |
| `Kneeboard/Services/DocumentService.cs` | Create | File picker, JSON deserialization, path resolution, validation |
| `Kneeboard/Services/IPdfService.cs` | Create | Interface: path → `IReadOnlyList<ImageSource>` |
| `Kneeboard/Services/INavigationService.cs` | Create | Interface wrapping `Shell.Current.GoToAsync` |
| `Kneeboard/Services/NavigationService.cs` | Create | Shell implementation |
| `Kneeboard/Services/ServiceHelper.cs` | Create | Static `IServiceProvider` accessor for XAML-created pages |
| `Kneeboard/Platforms/Windows/PdfService.cs` | Create | `Windows.Data.Pdf` eager render |
| `Kneeboard/Platforms/Android/PdfService.cs` | Create | `Android.Graphics.Pdf.PdfRenderer` eager render |
| `Kneeboard/ViewModels/SectionViewModel.cs` | Create | Per-section state: `Title`, `IsSelected`, `Pages` |
| `Kneeboard/ViewModels/WelcomeViewModel.cs` | Create | Open-file command, error message |
| `Kneeboard/ViewModels/KneeboardViewModel.cs` | Create | Sections, tab switching, page dots, re-open |
| `Kneeboard/ViewModels/MainViewModel.cs` | Delete | Replaced by WelcomeViewModel |
| `Kneeboard/Views/WelcomePage.xaml` | Create | Centered launch screen |
| `Kneeboard/Views/WelcomePage.xaml.cs` | Create | Constructor injection via ServiceHelper |
| `Kneeboard/Views/KneeboardPage.xaml` | Create | Tab bar + CarouselView + dots |
| `Kneeboard/Views/KneeboardPage.xaml.cs` | Create | Constructor injection via ServiceHelper |
| `Kneeboard/MainPage.xaml` | Delete | Replaced by WelcomePage |
| `Kneeboard/MainPage.xaml.cs` | Delete | Replaced by WelcomePage |
| `Kneeboard/AppShell.xaml` | Modify | Point to WelcomePage, register kneeboard route |
| `Kneeboard/AppShell.xaml.cs` | Modify | Register routes |
| `Kneeboard/MauiProgram.cs` | Modify | DI registrations, ServiceHelper init |
| `Kneeboard/App.xaml.cs` | Modify | Inject AppShell |

---

## Task 1: Test Project

**Files:**
- Create: `Kneeboard.Tests/Kneeboard.Tests.csproj`

- [ ] **Step 1: Create the test project file**

Create `Kneeboard.Tests/Kneeboard.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Kneeboard\Kneeboard.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add test project to solution**

Run from `C:\Users\tpase\src\kneeboard`:
```powershell
dotnet sln add Kneeboard.Tests/Kneeboard.Tests.csproj
```
Expected: `Project 'Kneeboard.Tests/Kneeboard.Tests.csproj' added to the solution.`

- [ ] **Step 3: Verify test project restores**

```powershell
dotnet restore Kneeboard.Tests/Kneeboard.Tests.csproj
```
Expected: `Restore succeeded.`

- [ ] **Step 4: Commit**

```powershell
git add Kneeboard.Tests/Kneeboard.Tests.csproj Kneeboard.sln Kneeboard.slnx
git commit -m "chore: add xUnit test project"
```

---

## Task 2: Models and JSON Deserialization

**Files:**
- Create: `Kneeboard/Models/KneeboardDocument.cs`
- Create: `Kneeboard/Models/ContentSource.cs`
- Create: `Kneeboard.Tests/Models/DocumentDeserializationTests.cs`

- [ ] **Step 1: Write failing deserialization tests**

Create `Kneeboard.Tests/Models/DocumentDeserializationTests.cs`:

```csharp
using System.Text.Json;
using Kneeboard.Models;
using Xunit;

namespace Kneeboard.Tests.Models;

public class DocumentDeserializationTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Deserialize_PdfSection_ParsesCorrectly()
    {
        const string json = """
            {
              "title": "EPKK 2026-06-10",
              "sections": [{
                "id": "datacard",
                "label": "Mission Datacard",
                "source": { "type": "pdf", "path": "/flights/datacard.pdf" }
              }]
            }
            """;

        var doc = JsonSerializer.Deserialize<KneeboardDocument>(json, Options)!;

        Assert.Equal("EPKK 2026-06-10", doc.Title);
        Assert.Single(doc.Sections);
        Assert.Equal("datacard", doc.Sections[0].Id);
        Assert.Equal("Mission Datacard", doc.Sections[0].Label);
        var src = Assert.IsType<PdfSource>(doc.Sections[0].Source);
        Assert.Equal("/flights/datacard.pdf", src.Path);
    }

    [Fact]
    public void Deserialize_ImageFolderSection_ParsesCorrectly()
    {
        const string json = """
            {
              "title": "T",
              "sections": [{
                "id": "airfields",
                "label": "Airfields Map",
                "source": { "type": "images", "folder": "/flights/airfields" }
              }]
            }
            """;

        var doc = JsonSerializer.Deserialize<KneeboardDocument>(json, Options)!;

        var src = Assert.IsType<ImageFolderSource>(doc.Sections[0].Source);
        Assert.Equal("/flights/airfields", src.Folder);
    }

    [Fact]
    public void Deserialize_MultipleSections_PreservesOrder()
    {
        const string json = """
            {
              "title": "T",
              "sections": [
                { "id":"a","label":"A","source":{"type":"images","folder":"/a"} },
                { "id":"b","label":"B","source":{"type":"pdf","path":"/b.pdf"} }
              ]
            }
            """;

        var doc = JsonSerializer.Deserialize<KneeboardDocument>(json, Options)!;

        Assert.Equal(2, doc.Sections.Count);
        Assert.IsType<ImageFolderSource>(doc.Sections[0].Source);
        Assert.IsType<PdfSource>(doc.Sections[1].Source);
    }

    [Fact]
    public void Deserialize_UnknownSourceType_ThrowsJsonException()
    {
        const string json = """
            {
              "title": "T",
              "sections": [{
                "id":"x","label":"X",
                "source":{"type":"video","url":"/x.mp4"}
              }]
            }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<KneeboardDocument>(json, Options));
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~DocumentDeserializationTests"
```
Expected: Build errors — `KneeboardDocument`, `PdfSource`, `ImageFolderSource` not defined.

- [ ] **Step 3: Create the model classes**

Create `Kneeboard/Models/KneeboardDocument.cs`:

```csharp
namespace Kneeboard.Models;

public class KneeboardDocument
{
    public string Title { get; set; } = string.Empty;
    public List<KneeboardSection> Sections { get; set; } = [];
}

public class KneeboardSection
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ContentSource Source { get; set; } = null!;
}
```

Create `Kneeboard/Models/ContentSource.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kneeboard.Models;

[JsonConverter(typeof(ContentSourceConverter))]
public abstract class ContentSource { }

public class PdfSource : ContentSource
{
    public string Path { get; set; } = string.Empty;
}

public class ImageFolderSource : ContentSource
{
    public string Folder { get; set; } = string.Empty;
}

public class ContentSourceConverter : JsonConverter<ContentSource>
{
    public override ContentSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString();
        return type switch
        {
            "pdf" => new PdfSource { Path = root.GetProperty("path").GetString()! },
            "images" => new ImageFolderSource { Folder = root.GetProperty("folder").GetString()! },
            _ => throw new JsonException($"Unknown source type: '{type}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentSource value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case PdfSource pdf:
                writer.WriteString("type", "pdf");
                writer.WriteString("path", pdf.Path);
                break;
            case ImageFolderSource img:
                writer.WriteString("type", "images");
                writer.WriteString("folder", img.Folder);
                break;
        }
        writer.WriteEndObject();
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~DocumentDeserializationTests"
```
Expected: `4 passed`.

- [ ] **Step 5: Commit**

```powershell
git add Kneeboard/Models/ Kneeboard.Tests/Models/
git commit -m "feat: add KneeboardDocument model and JSON deserialization"
```

---

## Task 3: DocumentService — Path Resolution and Validation

**Files:**
- Create: `Kneeboard/Services/IDocumentService.cs`
- Create: `Kneeboard/Services/DocumentService.cs`
- Create: `Kneeboard.Tests/Services/DocumentServiceTests.cs`

- [ ] **Step 1: Write failing service tests**

Create `Kneeboard.Tests/Services/DocumentServiceTests.cs`:

```csharp
using Kneeboard.Models;
using Kneeboard.Services;
using Xunit;

namespace Kneeboard.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public DocumentServiceTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task LoadFromPath_ResolvesRelativePdfPath()
    {
        var subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        var pdfPath = Path.Combine(subDir, "doc.pdf");
        await File.WriteAllBytesAsync(pdfPath, []);

        var json = """
            {
              "title":"T",
              "sections":[{"id":"a","label":"A","source":{"type":"pdf","path":"sub/doc.pdf"}}]
            }
            """;
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await new DocumentService().LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        var src = Assert.IsType<PdfSource>(result.Document!.Sections[0].Source);
        Assert.Equal(pdfPath, src.Path);
    }

    [Fact]
    public async Task LoadFromPath_AbsolutePdfPath_LeftUnchanged()
    {
        var pdfPath = Path.Combine(_tempDir, "doc.pdf");
        await File.WriteAllBytesAsync(pdfPath, []);

        var json = $$"""
            {"title":"T","sections":[{"id":"a","label":"A","source":{"type":"pdf","path":"{{pdfPath.Replace("\\", "\\\\")}}"}  }]}
            """;
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await new DocumentService().LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        var src = Assert.IsType<PdfSource>(result.Document!.Sections[0].Source);
        Assert.Equal(pdfPath, src.Path);
    }

    [Fact]
    public async Task LoadFromPath_ResolvesRelativeImageFolder()
    {
        var folder = Path.Combine(_tempDir, "images");
        Directory.CreateDirectory(folder);

        var json = """
            {"title":"T","sections":[{"id":"a","label":"A","source":{"type":"images","folder":"images"}}]}
            """;
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await new DocumentService().LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        var src = Assert.IsType<ImageFolderSource>(result.Document!.Sections[0].Source);
        Assert.Equal(folder, src.Folder);
    }

    [Fact]
    public async Task LoadFromPath_MissingPdf_ReturnsErrorNamingSection()
    {
        var json = """
            {"title":"T","sections":[{"id":"a","label":"Mission Datacard","source":{"type":"pdf","path":"/nonexistent/x.pdf"}}]}
            """;
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await new DocumentService().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.False(result.Cancelled);
        Assert.Contains("Mission Datacard", result.Error);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task LoadFromPath_MissingImageFolder_ReturnsError()
    {
        var json = """
            {"title":"T","sections":[{"id":"a","label":"Airfields Map","source":{"type":"images","folder":"/nonexistent/dir"}}]}
            """;
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await new DocumentService().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.Contains("Airfields Map", result.Error);
    }

    [Fact]
    public async Task LoadFromPath_InvalidJson_ReturnsError()
    {
        var docFile = Path.Combine(_tempDir, "bad.kneeboard");
        await File.WriteAllTextAsync(docFile, "this is not json");

        var result = await new DocumentService().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.Contains("valid JSON", result.Error);
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~DocumentServiceTests"
```
Expected: Build error — `DocumentService`, `IDocumentService` not defined.

- [ ] **Step 3: Create the interface and result type**

Create `Kneeboard/Services/IDocumentService.cs`:

```csharp
using Kneeboard.Models;

namespace Kneeboard.Services;

public interface IDocumentService
{
    Task<DocumentLoadResult> PickAndLoadAsync();
    Task<DocumentLoadResult> LoadFromPathAsync(string filePath);
}

public class DocumentLoadResult
{
    public KneeboardDocument? Document { get; init; }
    public string? Error { get; init; }
    public bool Cancelled { get; init; }

    public bool Success => Document != null;

    public static DocumentLoadResult Succeeded(KneeboardDocument doc) => new() { Document = doc };
    public static DocumentLoadResult Failed(string error) => new() { Error = error };
    public static DocumentLoadResult Cancelled() => new() { Cancelled = true };
}
```

- [ ] **Step 4: Create DocumentService**

Create `Kneeboard/Services/DocumentService.cs`:

```csharp
using System.Text.Json;
using Kneeboard.Models;

namespace Kneeboard.Services;

public class DocumentService : IDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<DocumentLoadResult> PickAndLoadAsync()
    {
        var picked = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Open Kneeboard File",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".kneeboard"] },
                { DevicePlatform.Android, ["application/octet-stream"] },
            })
        });

        return picked is null
            ? DocumentLoadResult.Cancelled()
            : await LoadFromPathAsync(picked.FullPath);
    }

    public async Task<DocumentLoadResult> LoadFromPathAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var document = JsonSerializer.Deserialize<KneeboardDocument>(json, JsonOptions);

            if (document is null)
                return DocumentLoadResult.Failed("Could not read kneeboard file. Check that it's valid JSON.");

            var baseDir = Path.GetDirectoryName(filePath)!;
            ResolveRelativePaths(document, baseDir);

            var error = Validate(document);
            return error is null
                ? DocumentLoadResult.Succeeded(document)
                : DocumentLoadResult.Failed(error);
        }
        catch (JsonException)
        {
            return DocumentLoadResult.Failed("Could not read kneeboard file. Check that it's valid JSON.");
        }
        catch (Exception ex)
        {
            return DocumentLoadResult.Failed($"Could not open file: {ex.Message}");
        }
    }

    private static void ResolveRelativePaths(KneeboardDocument document, string baseDir)
    {
        foreach (var section in document.Sections)
        {
            switch (section.Source)
            {
                case PdfSource pdf when !Path.IsPathRooted(pdf.Path):
                    pdf.Path = Path.GetFullPath(Path.Combine(baseDir, pdf.Path));
                    break;
                case ImageFolderSource img when !Path.IsPathRooted(img.Folder):
                    img.Folder = Path.GetFullPath(Path.Combine(baseDir, img.Folder));
                    break;
            }
        }
    }

    private static string? Validate(KneeboardDocument document)
    {
        foreach (var section in document.Sections)
        {
            switch (section.Source)
            {
                case PdfSource pdf when !File.Exists(pdf.Path):
                    return $"{section.Label}: file not found at {pdf.Path}";
                case ImageFolderSource img when !Directory.Exists(img.Folder):
                    return $"{section.Label}: folder not found at {img.Folder}";
            }
        }
        return null;
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~DocumentServiceTests"
```
Expected: `6 passed`.

- [ ] **Step 6: Commit**

```powershell
git add Kneeboard/Services/IDocumentService.cs Kneeboard/Services/DocumentService.cs Kneeboard.Tests/Services/
git commit -m "feat: add DocumentService with path resolution and validation"
```

---

## Task 4: IPdfService, INavigationService, ServiceHelper

**Files:**
- Create: `Kneeboard/Services/IPdfService.cs`
- Create: `Kneeboard/Services/INavigationService.cs`
- Create: `Kneeboard/Services/NavigationService.cs`
- Create: `Kneeboard/Services/ServiceHelper.cs`

No tests for these — they are thin wrappers around MAUI infrastructure or simple interfaces.

- [ ] **Step 1: Create IPdfService**

Create `Kneeboard/Services/IPdfService.cs`:

```csharp
namespace Kneeboard.Services;

public interface IPdfService
{
    Task<IReadOnlyList<ImageSource>> RenderAllPagesAsync(string pdfPath);
}
```

- [ ] **Step 2: Create INavigationService and NavigationService**

Create `Kneeboard/Services/INavigationService.cs`:

```csharp
namespace Kneeboard.Services;

public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
}
```

Create `Kneeboard/Services/NavigationService.cs`:

```csharp
namespace Kneeboard.Services;

public class NavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
        parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, parameters);
}
```

- [ ] **Step 3: Create ServiceHelper**

MAUI's `ContentTemplate` in `AppShell.xaml` creates pages using the default constructor (not DI). `ServiceHelper` provides a static escape hatch so pages can self-resolve their ViewModel.

Create `Kneeboard/Services/ServiceHelper.cs`:

```csharp
namespace Kneeboard.Services;

public static class ServiceHelper
{
    private static IServiceProvider? _services;

    public static IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("ServiceHelper not initialized. Call Initialize() in MauiProgram.");

    public static void Initialize(IServiceProvider services) => _services = services;

    public static T GetRequired<T>() where T : notnull =>
        Services.GetRequiredService<T>();
}
```

- [ ] **Step 4: Commit**

```powershell
git add Kneeboard/Services/IPdfService.cs Kneeboard/Services/INavigationService.cs Kneeboard/Services/NavigationService.cs Kneeboard/Services/ServiceHelper.cs
git commit -m "feat: add IPdfService, INavigationService, ServiceHelper"
```

---

## Task 5: PdfService — Windows

**Files:**
- Create: `Kneeboard/Platforms/Windows/PdfService.cs`

- [ ] **Step 1: Create the Windows PDF renderer**

Create `Kneeboard/Platforms/Windows/PdfService.cs`:

```csharp
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Kneeboard.Services;

namespace Kneeboard.Platforms.Windows;

public class PdfService : IPdfService
{
    public async Task<IReadOnlyList<ImageSource>> RenderAllPagesAsync(string pdfPath)
    {
        var file = await StorageFile.GetFileFromPathAsync(pdfPath);
        var pdfDocument = await PdfDocument.LoadFromFileAsync(file);
        var pages = new List<ImageSource>((int)pdfDocument.PageCount);

        for (uint i = 0; i < pdfDocument.PageCount; i++)
        {
            try
            {
                using var page = pdfDocument.GetPage(i);
                using var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream);

                var reader = new DataReader(stream.GetInputStreamAt(0));
                await reader.LoadAsync((uint)stream.Size);
                var bytes = new byte[stream.Size];
                reader.ReadBytes(bytes);

                pages.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
            }
            catch
            {
                // Failed pages are silently skipped; the rest of the document still loads.
            }
        }

        return pages;
    }
}
```

- [ ] **Step 2: Build Windows target to verify it compiles**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-windows10.0.19041.0
```
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Commit**

```powershell
git add Kneeboard/Platforms/Windows/PdfService.cs
git commit -m "feat: add Windows PDF renderer using Windows.Data.Pdf"
```

---

## Task 6: PdfService — Android

**Files:**
- Create: `Kneeboard/Platforms/Android/PdfService.cs`

- [ ] **Step 1: Create the Android PDF renderer**

Create `Kneeboard/Platforms/Android/PdfService.cs`:

```csharp
using Android.Graphics;
using Android.Graphics.Pdf;
using Kneeboard.Services;

namespace Kneeboard.Platforms.Android;

public class PdfService : IPdfService
{
    public Task<IReadOnlyList<ImageSource>> RenderAllPagesAsync(string pdfPath)
    {
        using var fd = Android.OS.ParcelFileDescriptor.Open(
            new Java.IO.File(pdfPath),
            Android.OS.ParcelFileMode.ReadOnly);

        using var renderer = new PdfRenderer(fd!);
        var pages = new List<ImageSource>(renderer.PageCount);

        for (int i = 0; i < renderer.PageCount; i++)
        {
            try
            {
                using var page = renderer.OpenPage(i);
                using var bitmap = Bitmap.CreateBitmap(page.Width, page.Height, Bitmap.Config.Argb8888!)!;
                page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);

                using var ms = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Png!, 100, ms);
                var bytes = ms.ToArray();

                pages.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
            }
            catch
            {
                // Failed pages are silently skipped; the rest of the document still loads.
            }
        }

        return Task.FromResult<IReadOnlyList<ImageSource>>(pages);
    }
}
```

- [ ] **Step 2: Build Android target to verify it compiles**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-android
```
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Commit**

```powershell
git add Kneeboard/Platforms/Android/PdfService.cs
git commit -m "feat: add Android PDF renderer using PdfRenderer"
```

---

## Task 7: WelcomeViewModel

**Files:**
- Create: `Kneeboard/ViewModels/SectionViewModel.cs`
- Create: `Kneeboard/ViewModels/WelcomeViewModel.cs`
- Create: `Kneeboard.Tests/ViewModels/WelcomeViewModelTests.cs`

- [ ] **Step 1: Write failing ViewModel tests**

Create `Kneeboard.Tests/ViewModels/WelcomeViewModelTests.cs`:

```csharp
using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class WelcomeViewModelTests
{
    [Fact]
    public async Task OpenFile_WhenCancelled_DoesNotNavigateAndClearsError()
    {
        var vm = BuildVm(DocumentLoadResult.Cancelled());

        await vm.OpenFileCommand.ExecuteAsync(null);

        Assert.Empty(_nav.Routes);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task OpenFile_WhenFailed_SetsErrorMessage()
    {
        var vm = BuildVm(DocumentLoadResult.Failed("bad file"));

        await vm.OpenFileCommand.ExecuteAsync(null);

        Assert.Equal("bad file", vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.Empty(_nav.Routes);
    }

    [Fact]
    public async Task OpenFile_WhenSuccessful_NavigatesToKneeboard()
    {
        var doc = new KneeboardDocument { Title = "T", Sections = [] };
        var vm = BuildVm(DocumentLoadResult.Succeeded(doc));

        await vm.OpenFileCommand.ExecuteAsync(null);

        Assert.Single(_nav.Routes);
        Assert.Equal("kneeboard", _nav.Routes[0]);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task OpenFile_DuringLoad_IsBusyIsTrue()
    {
        var tcs = new TaskCompletionSource<DocumentLoadResult>();
        var vm = new WelcomeViewModel(new TcsDocumentService(tcs), _nav);
        bool busyDuringLoad = false;

        var task = vm.OpenFileCommand.ExecuteAsync(null);
        busyDuringLoad = vm.IsBusy;
        tcs.SetResult(DocumentLoadResult.Cancelled());
        await task;

        Assert.True(busyDuringLoad);
        Assert.False(vm.IsBusy);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private readonly SpyNavigationService _nav = new();

    private WelcomeViewModel BuildVm(DocumentLoadResult result) =>
        new(new StubDocumentService(result), _nav);

    private class StubDocumentService(DocumentLoadResult result) : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => Task.FromResult(result);
        public Task<DocumentLoadResult> LoadFromPathAsync(string path) => Task.FromResult(result);
    }

    private class TcsDocumentService(TaskCompletionSource<DocumentLoadResult> tcs) : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => tcs.Task;
        public Task<DocumentLoadResult> LoadFromPathAsync(string path) => tcs.Task;
    }

    private class SpyNavigationService : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            Routes.Add(route);
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~WelcomeViewModelTests"
```
Expected: Build error — `WelcomeViewModel` not defined.

- [ ] **Step 3: Create SectionViewModel**

Create `Kneeboard/ViewModels/SectionViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Kneeboard.Models;

namespace Kneeboard.ViewModels;

public partial class SectionViewModel : BaseViewModel
{
    public KneeboardSection Section { get; }

    [ObservableProperty]
    private bool isSelected;

    public IReadOnlyList<ImageSource> Pages { get; set; } = [];

    public SectionViewModel(KneeboardSection section)
    {
        Section = section;
        Title = section.Label;
    }
}
```

- [ ] **Step 4: Create WelcomeViewModel**

Create `Kneeboard/ViewModels/WelcomeViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly IDocumentService _documentService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public WelcomeViewModel(IDocumentService documentService, INavigationService navigation)
    {
        _documentService = documentService;
        _navigation = navigation;
        Title = "Kneeboard";
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _documentService.PickAndLoadAsync();

            if (result.Cancelled) return;

            if (!result.Success)
            {
                ErrorMessage = result.Error;
                return;
            }

            await _navigation.GoToAsync("kneeboard", new Dictionary<string, object>
            {
                ["Document"] = result.Document!
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~WelcomeViewModelTests"
```
Expected: `4 passed`.

- [ ] **Step 6: Commit**

```powershell
git add Kneeboard/ViewModels/SectionViewModel.cs Kneeboard/ViewModels/WelcomeViewModel.cs Kneeboard.Tests/ViewModels/WelcomeViewModelTests.cs
git commit -m "feat: add WelcomeViewModel and SectionViewModel"
```

---

## Task 8: WelcomePage XAML

**Files:**
- Create: `Kneeboard/Views/WelcomePage.xaml`
- Create: `Kneeboard/Views/WelcomePage.xaml.cs`

- [ ] **Step 1: Create WelcomePage.xaml**

Create `Kneeboard/Views/WelcomePage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Kneeboard.ViewModels"
             x:Class="Kneeboard.Views.WelcomePage"
             x:DataType="vm:WelcomeViewModel"
             Shell.NavBarIsVisible="False"
             BackgroundColor="#111827">

    <Grid RowDefinitions="*,Auto,*">

        <VerticalStackLayout Grid.Row="1"
                             HorizontalOptions="Center"
                             Spacing="16">

            <Label Text="✈ KNEEBOARD"
                   FontSize="32"
                   FontAttributes="Bold"
                   TextColor="#E94560"
                   HorizontalOptions="Center"
                   CharacterSpacing="3" />

            <Label Text="Ready for preflight"
                   FontSize="13"
                   TextColor="#6B7280"
                   HorizontalOptions="Center" />

            <Button Text="OPEN KNEEBOARD FILE…"
                    Command="{Binding OpenFileCommand}"
                    IsEnabled="{Binding IsNotBusy}"
                    BackgroundColor="#E94560"
                    TextColor="White"
                    FontAttributes="Bold"
                    CharacterSpacing="1"
                    CornerRadius="6"
                    Padding="28,10"
                    Margin="0,12,0,0" />

            <Label Text="{Binding ErrorMessage}"
                   IsVisible="{Binding HasError}"
                   TextColor="#F87171"
                   FontSize="13"
                   HorizontalOptions="Center"
                   HorizontalTextAlignment="Center"
                   MaximumWidthRequest="400" />

            <ActivityIndicator IsRunning="{Binding IsBusy}"
                               IsVisible="{Binding IsBusy}"
                               Color="#E94560"
                               HorizontalOptions="Center" />

        </VerticalStackLayout>

    </Grid>

</ContentPage>
```

- [ ] **Step 2: Create WelcomePage.xaml.cs**

Create `Kneeboard/Views/WelcomePage.xaml.cs`:

```csharp
using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class WelcomePage : ContentPage
{
    public WelcomePage() : this(ServiceHelper.GetRequired<WelcomeViewModel>()) { }

    public WelcomePage(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3: Build to verify XAML compiles**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-windows10.0.19041.0
```
Expected: `Build succeeded.` — ignore any "WelcomePage is not the root" warnings for now (the shell hasn't been updated yet).

- [ ] **Step 4: Commit**

```powershell
git add Kneeboard/Views/WelcomePage.xaml Kneeboard/Views/WelcomePage.xaml.cs
git commit -m "feat: add WelcomePage XAML"
```

---

## Task 9: KneeboardViewModel

**Files:**
- Create: `Kneeboard/ViewModels/KneeboardViewModel.cs`
- Create: `Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs`

- [ ] **Step 1: Write failing ViewModel tests**

Create `Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs`:

```csharp
using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class KneeboardViewModelTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var d in _tempDirs)
            if (Directory.Exists(d)) Directory.Delete(d, recursive: true);
    }

    [Fact]
    public void SetDocument_LoadsAllSections_FirstSelected()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());

        vm.Document = doc;

        Assert.Equal(2, vm.Sections.Count);
        Assert.Equal(0, vm.SelectedSectionIndex);
        Assert.True(vm.Sections[0].IsSelected);
        Assert.False(vm.Sections[1].IsSelected);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SetDocument_SetsTitle()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());

        vm.Document = doc;

        Assert.Equal("EPKK 2026-06-10", vm.Title);
    }

    [Fact]
    public void SelectSection_UpdatesSelectedIndexAndIsSelected()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = doc;

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(1, vm.SelectedSectionIndex);
        Assert.False(vm.Sections[0].IsSelected);
        Assert.True(vm.Sections[1].IsSelected);
    }

    [Fact]
    public void SelectSection_ResetsPageIndexToZero()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = doc;
        vm.CurrentPageIndex = 3;

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(0, vm.CurrentPageIndex);
    }

    [Fact]
    public void CurrentPageDots_ReflectsCurrentPageIndex()
    {
        var folder = CreateTempFolder();
        File.WriteAllText(Path.Combine(folder, "p1.png"), "");
        File.WriteAllText(Path.Combine(folder, "p2.png"), "");
        File.WriteAllText(Path.Combine(folder, "p3.png"), "");

        var doc = new KneeboardDocument
        {
            Title = "T",
            Sections =
            [
                new() { Id = "a", Label = "A", Source = new ImageFolderSource { Folder = folder } }
            ]
        };
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = doc;

        vm.CurrentPageIndex = 1;

        Assert.Equal(3, vm.CurrentPageDots.Count);
        Assert.False(vm.CurrentPageDots[0]);
        Assert.True(vm.CurrentPageDots[1]);
        Assert.False(vm.CurrentPageDots[2]);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private KneeboardDocument TwoSectionDoc() => new()
    {
        Title = "EPKK 2026-06-10",
        Sections =
        [
            new() { Id = "a", Label = "A", Source = new ImageFolderSource { Folder = CreateTempFolder() } },
            new() { Id = "b", Label = "B", Source = new ImageFolderSource { Folder = CreateTempFolder() } }
        ]
    };

    private string CreateTempFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private class StubDocumentService : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => Task.FromResult(DocumentLoadResult.Cancelled());
        public Task<DocumentLoadResult> LoadFromPathAsync(string p) => Task.FromResult(DocumentLoadResult.Cancelled());
    }

    private class StubPdfService : IPdfService
    {
        public Task<IReadOnlyList<ImageSource>> RenderAllPagesAsync(string pdfPath) =>
            Task.FromResult<IReadOnlyList<ImageSource>>([]);
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~KneeboardViewModelTests"
```
Expected: Build error — `KneeboardViewModel` not defined.

- [ ] **Step 3: Create KneeboardViewModel**

Create `Kneeboard/ViewModels/KneeboardViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kneeboard.Models;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

[QueryProperty(nameof(Document), "Document")]
public partial class KneeboardViewModel : BaseViewModel
{
    private readonly IDocumentService _documentService;
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    private KneeboardDocument? document;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPages))]
    [NotifyPropertyChangedFor(nameof(CurrentPageDots))]
    private int selectedSectionIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPageDots))]
    private int currentPageIndex;

    [ObservableProperty]
    private List<SectionViewModel> sections = [];

    [ObservableProperty]
    private bool isLoading;

    public IReadOnlyList<ImageSource> CurrentPages =>
        SelectedSectionIndex >= 0 && SelectedSectionIndex < Sections.Count
            ? Sections[SelectedSectionIndex].Pages
            : [];

    public IReadOnlyList<bool> CurrentPageDots =>
        CurrentPages.Select((_, i) => i == CurrentPageIndex).ToList();

    public KneeboardViewModel(IDocumentService documentService, IPdfService pdfService)
    {
        _documentService = documentService;
        _pdfService = pdfService;
    }

    partial void OnDocumentChanged(KneeboardDocument? value)
    {
        if (value is not null)
            _ = LoadDocumentAsync(value);
    }

    partial void OnSelectedSectionIndexChanged(int value)
    {
        for (var i = 0; i < Sections.Count; i++)
            Sections[i].IsSelected = i == value;
        CurrentPageIndex = 0;
    }

    private async Task LoadDocumentAsync(KneeboardDocument doc)
    {
        IsLoading = true;
        Title = doc.Title;

        var sectionVMs = doc.Sections.Select(s => new SectionViewModel(s)).ToList();

        foreach (var vm in sectionVMs)
        {
            vm.Pages = vm.Section.Source switch
            {
                PdfSource pdf => await _pdfService.RenderAllPagesAsync(pdf.Path),
                ImageFolderSource img => LoadImagesFromFolder(img.Folder),
                _ => []
            };
        }

        Sections = sectionVMs;

        // Set backing fields directly to avoid no-change guard on index 0 → 0
        selectedSectionIndex = 0;
        currentPageIndex = 0;
        if (sectionVMs.Count > 0) sectionVMs[0].IsSelected = true;

        OnPropertyChanged(nameof(SelectedSectionIndex));
        OnPropertyChanged(nameof(CurrentPageIndex));
        OnPropertyChanged(nameof(CurrentPages));
        OnPropertyChanged(nameof(CurrentPageDots));

        IsLoading = false;
    }

    private static IReadOnlyList<ImageSource> LoadImagesFromFolder(string folder)
    {
        string[] extensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"];
        return [.. Directory.GetFiles(folder)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Order()
            .Select(ImageSource.FromFile)];
    }

    [RelayCommand]
    private void SelectSection(SectionViewModel section)
    {
        var index = Sections.IndexOf(section);
        if (index >= 0)
            SelectedSectionIndex = index;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var result = await _documentService.PickAndLoadAsync();
        if (result.Success)
            Document = result.Document;
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~KneeboardViewModelTests"
```
Expected: `5 passed`.

- [ ] **Step 5: Commit**

```powershell
git add Kneeboard/ViewModels/KneeboardViewModel.cs Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs
git commit -m "feat: add KneeboardViewModel with section switching and page dots"
```

---

## Task 10: KneeboardPage XAML

**Files:**
- Create: `Kneeboard/Views/KneeboardPage.xaml`
- Create: `Kneeboard/Views/KneeboardPage.xaml.cs`

- [ ] **Step 1: Create KneeboardPage.xaml**

Create `Kneeboard/Views/KneeboardPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             xmlns:vm="clr-namespace:Kneeboard.ViewModels"
             x:Class="Kneeboard.Views.KneeboardPage"
             x:DataType="vm:KneeboardViewModel"
             x:Name="Self"
             Shell.NavBarIsVisible="False"
             BackgroundColor="#0D1117">

    <ContentPage.Resources>
        <Color x:Key="ActiveDotColor">#E94560</Color>
        <Color x:Key="InactiveDotColor">#374151</Color>
        <toolkit:BoolToObjectConverter x:Key="DotColorConverter"
                                       TrueObject="{StaticResource ActiveDotColor}"
                                       FalseObject="{StaticResource InactiveDotColor}" />
    </ContentPage.Resources>

    <Grid RowDefinitions="Auto,*,Auto">

        <!-- Tab bar -->
        <Grid Grid.Row="0"
              ColumnDefinitions="*,Auto"
              BackgroundColor="#1F2937">

            <HorizontalStackLayout Grid.Column="0"
                                   BindableLayout.ItemsSource="{Binding Sections}">
                <BindableLayout.ItemTemplate>
                    <DataTemplate x:DataType="vm:SectionViewModel">
                        <Button Text="{Binding Title}"
                                BackgroundColor="Transparent"
                                TextColor="#6B7280"
                                FontSize="12"
                                FontAttributes="Bold"
                                CharacterSpacing="0.5"
                                Padding="20,10"
                                BorderWidth="0"
                                Command="{Binding Source={x:Reference Self}, Path=BindingContext.SelectSectionCommand}"
                                CommandParameter="{Binding .}">
                            <Button.Triggers>
                                <DataTrigger TargetType="Button"
                                             Binding="{Binding IsSelected}"
                                             Value="True">
                                    <Setter Property="TextColor" Value="White" />
                                    <Setter Property="BackgroundColor" Value="#E94560" />
                                </DataTrigger>
                            </Button.Triggers>
                        </Button>
                    </DataTemplate>
                </BindableLayout.ItemTemplate>
            </HorizontalStackLayout>

            <HorizontalStackLayout Grid.Column="1"
                                   VerticalOptions="Center"
                                   Spacing="8"
                                   Padding="0,0,12,0">
                <Label Text="{Binding Title}"
                       TextColor="#4B5563"
                       FontSize="11"
                       VerticalOptions="Center" />
                <Button Text="📂"
                        Command="{Binding OpenFileCommand}"
                        BackgroundColor="Transparent"
                        TextColor="#4B5563"
                        Padding="6,4"
                        BorderWidth="0" />
            </HorizontalStackLayout>

        </Grid>

        <!-- Content area -->
        <Grid Grid.Row="1">

            <CarouselView ItemsSource="{Binding CurrentPages}"
                          Position="{Binding CurrentPageIndex, Mode=TwoWay}"
                          Loop="False"
                          HorizontalScrollBarVisibility="Never">
                <CarouselView.ItemTemplate>
                    <DataTemplate>
                        <Image Source="{Binding .}"
                               Aspect="AspectFit"
                               HorizontalOptions="Fill"
                               VerticalOptions="Fill" />
                    </DataTemplate>
                </CarouselView.ItemTemplate>
                <CarouselView.EmptyView>
                    <Label Text="No pages"
                           TextColor="#6B7280"
                           FontSize="16"
                           HorizontalOptions="Center"
                           VerticalOptions="Center" />
                </CarouselView.EmptyView>
            </CarouselView>

            <!-- Loading overlay -->
            <Grid IsVisible="{Binding IsLoading}"
                  BackgroundColor="#CC000000">
                <ActivityIndicator IsRunning="{Binding IsLoading}"
                                   Color="#E94560"
                                   HorizontalOptions="Center"
                                   VerticalOptions="Center" />
            </Grid>

        </Grid>

        <!-- Page indicator dots -->
        <HorizontalStackLayout Grid.Row="2"
                               HorizontalOptions="Center"
                               Padding="0,8"
                               BackgroundColor="#0D1117"
                               BindableLayout.ItemsSource="{Binding CurrentPageDots}">
            <BindableLayout.ItemTemplate>
                <DataTemplate x:DataType="x:Boolean">
                    <BoxView WidthRequest="6"
                             HeightRequest="6"
                             CornerRadius="3"
                             Margin="3,0"
                             Color="{Binding ., Converter={StaticResource DotColorConverter}}" />
                </DataTemplate>
            </BindableLayout.ItemTemplate>
        </HorizontalStackLayout>

    </Grid>

</ContentPage>
```

- [ ] **Step 2: Create KneeboardPage.xaml.cs**

Create `Kneeboard/Views/KneeboardPage.xaml.cs`:

```csharp
using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class KneeboardPage : ContentPage
{
    public KneeboardPage() : this(ServiceHelper.GetRequired<KneeboardViewModel>()) { }

    public KneeboardPage(KneeboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3: Build to verify XAML compiles**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-windows10.0.19041.0
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```powershell
git add Kneeboard/Views/KneeboardPage.xaml Kneeboard/Views/KneeboardPage.xaml.cs
git commit -m "feat: add KneeboardPage XAML with tab bar, CarouselView and dot indicator"
```

---

## Task 11: Shell, DI Wiring, and Cleanup

**Files:**
- Modify: `Kneeboard/AppShell.xaml`
- Modify: `Kneeboard/AppShell.xaml.cs`
- Modify: `Kneeboard/MauiProgram.cs`
- Modify: `Kneeboard/App.xaml.cs`
- Delete: `Kneeboard/MainPage.xaml`
- Delete: `Kneeboard/MainPage.xaml.cs`
- Delete: `Kneeboard/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Update AppShell.xaml**

Replace the entire contents of `Kneeboard/AppShell.xaml`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="Kneeboard.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:views="clr-namespace:Kneeboard.Views"
    Shell.FlyoutBehavior="Disabled">

    <ShellContent
        Route="welcome"
        ContentTemplate="{DataTemplate views:WelcomePage}" />

</Shell>
```

- [ ] **Step 2: Update AppShell.xaml.cs**

Replace the entire contents of `Kneeboard/AppShell.xaml.cs`:

```csharp
using Kneeboard.Views;

namespace Kneeboard;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("kneeboard", typeof(KneeboardPage));
    }
}
```

- [ ] **Step 3: Update MauiProgram.cs**

Replace the entire contents of `Kneeboard/MauiProgram.cs`:

```csharp
using CommunityToolkit.Maui;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Kneeboard.Views;
using Microsoft.Extensions.Logging;

namespace Kneeboard;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IDocumentService, DocumentService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

#if WINDOWS
        builder.Services.AddSingleton<IPdfService, Platforms.Windows.PdfService>();
#elif ANDROID
        builder.Services.AddSingleton<IPdfService, Platforms.Android.PdfService>();
#endif

        // Pages + ViewModels (transient: fresh instance each navigation)
        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<KneeboardViewModel>();
        builder.Services.AddTransient<KneeboardPage>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
```

- [ ] **Step 4: Update App.xaml.cs**

Replace the entire contents of `Kneeboard/App.xaml.cs`:

```csharp
using Kneeboard.Services;

namespace Kneeboard;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(ServiceHelper.GetRequired<AppShell>());
}
```

- [ ] **Step 5: Delete MainPage and MainViewModel**

```powershell
Remove-Item Kneeboard/MainPage.xaml
Remove-Item Kneeboard/MainPage.xaml.cs
Remove-Item Kneeboard/ViewModels/MainViewModel.cs
```

- [ ] **Step 6: Remove gitkeep placeholders now that real files exist**

```powershell
Remove-Item Kneeboard/Views/.gitkeep -ErrorAction SilentlyContinue
Remove-Item Kneeboard/Models/.gitkeep -ErrorAction SilentlyContinue
```

- [ ] **Step 7: Build both targets**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-windows10.0.19041.0
```
Expected: `Build succeeded.` 0 errors.

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-android
```
Expected: `Build succeeded.` 0 errors.

- [ ] **Step 8: Run all tests**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0
```
Expected: `19 passed` (4 deserialization + 6 service + 4 welcome VM + 5 kneeboard VM).

- [ ] **Step 9: Commit**

```powershell
git add -A
git commit -m "feat: wire Shell navigation and DI, remove MainPage scaffold"
```

---

## Task 12: Build Verification

- [ ] **Step 1: Full clean build — Windows**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-windows10.0.19041.0 --no-incremental
```
Expected: `Build succeeded.` 0 errors, 0 warnings about missing types.

- [ ] **Step 2: Full clean build — Android**

```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-android --no-incremental
```
Expected: `Build succeeded.` 0 errors.

- [ ] **Step 3: All tests green**

```powershell
dotnet test Kneeboard.Tests/Kneeboard.Tests.csproj -f net10.0-windows10.0.19041.0 --no-build
```
Expected: All tests pass, 0 failed.

- [ ] **Step 4: Commit**

```powershell
git tag v0.1.0-sda
git commit --allow-empty -m "chore: v0.1.0-sda — single document app complete"
```
