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
                case null:
                    return $"{section.Label}: missing 'source' field.";
            }
        }
        return null;
    }
}
