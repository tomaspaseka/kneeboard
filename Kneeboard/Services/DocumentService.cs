using System.Diagnostics;
using System.Text.Json;
using Kneeboard.Models;

namespace Kneeboard.Services;

public class DocumentService(IRecentDocumentsService recentDocumentsService) : IDocumentService
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
            if (error is not null)
                return DocumentLoadResult.Failed(error);

            await RecordOpenedAsync(document, filePath);
            return DocumentLoadResult.Succeeded(document);
        }
        catch (JsonException)
        {
            return DocumentLoadResult.Failed("Could not read kneeboard file. Check that it's valid JSON.");
        }
        catch (FileNotFoundException)
        {
            return DocumentLoadResult.Failed($"Kneeboard file not found: {filePath}");
        }
        catch (Exception ex)
        {
            return DocumentLoadResult.Failed($"Could not open file: {ex.Message}");
        }
    }

    // A recents-store hiccup must never turn an otherwise-successful load into a reported failure;
    // it's swallowed (and logged for diagnosis) rather than surfaced to the user.
    private async Task RecordOpenedAsync(KneeboardDocument document, string filePath)
    {
        try
        {
            var title = string.IsNullOrWhiteSpace(document.Title)
                ? Path.GetFileNameWithoutExtension(filePath)
                : document.Title;

            await recentDocumentsService.RecordOpenedAsync(
                new RecentDocument(filePath, title, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to record recent document '{filePath}': {ex}");
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
                case null:
                    return $"{section.Label}: missing 'source' field.";
            }
        }
        return null;
    }
}
