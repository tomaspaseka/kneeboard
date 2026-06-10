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

        var escapedPath = pdfPath.Replace("\\", "\\\\");
        var json = "{\"title\":\"T\",\"sections\":[{\"id\":\"a\",\"label\":\"A\",\"source\":{\"type\":\"pdf\",\"path\":\"" + escapedPath + "\"}}]}";
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
        Assert.False(result.WasCancelled);
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
