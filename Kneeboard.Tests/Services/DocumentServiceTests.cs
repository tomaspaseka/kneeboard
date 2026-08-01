using Kneeboard.Models;
using Kneeboard.Services;
using Xunit;

namespace Kneeboard.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly SpyRecentDocumentsService _recentDocuments = new();

    public DocumentServiceTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private DocumentService BuildSut() => new(_recentDocuments);

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

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        var src = Assert.IsType<PdfSource>(result.Document!.Sections[0].Source);
        Assert.Equal(pdfPath, src.Path);
        Assert.Single(_recentDocuments.Recorded);
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

        var result = await BuildSut().LoadFromPathAsync(docFile);

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

        var result = await BuildSut().LoadFromPathAsync(docFile);

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

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.False(result.WasCancelled);
        Assert.Contains("Mission Datacard", result.Error);
        Assert.Contains("not found", result.Error);
        Assert.Empty(_recentDocuments.Recorded);
    }

    [Fact]
    public async Task LoadFromPath_MissingImageFolder_ReturnsError()
    {
        var json = """
            {"title":"T","sections":[{"id":"a","label":"Airfields Map","source":{"type":"images","folder":"/nonexistent/dir"}}]}
            """;
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.Contains("Airfields Map", result.Error);
        Assert.Empty(_recentDocuments.Recorded);
    }

    [Fact]
    public async Task LoadFromPath_InvalidJson_ReturnsError()
    {
        var docFile = Path.Combine(_tempDir, "bad.kneeboard");
        await File.WriteAllTextAsync(docFile, "this is not json");

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.Contains("valid JSON", result.Error);
        Assert.Empty(_recentDocuments.Recorded);
    }

    [Fact]
    public async Task LoadFromPath_FileNotFound_ReturnsError_AndDoesNotRecordRecent()
    {
        var docFile = Path.Combine(_tempDir, "missing.kneeboard");

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error);
        Assert.Empty(_recentDocuments.Recorded);
    }

    [Fact]
    public async Task LoadFromPath_Success_RecordsRecentDocumentExactlyOnce()
    {
        var json = """{"title":"Mission Plan","sections":[]}""";
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        var recorded = Assert.Single(_recentDocuments.Recorded);
        Assert.Equal(docFile, recorded.Path);
        Assert.Equal("Mission Plan", recorded.Title);
    }

    [Fact]
    public async Task LoadFromPath_Success_BlankTitle_RecordsFileNameAsTitle()
    {
        var json = """{"title":"","sections":[]}""";
        var docFile = Path.Combine(_tempDir, "sortie-brief.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var result = await BuildSut().LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        var recorded = Assert.Single(_recentDocuments.Recorded);
        Assert.Equal("sortie-brief", recorded.Title);
    }

    [Fact]
    public async Task LoadFromPath_Success_RecordingRecentThrows_LoadStillSucceeds()
    {
        var json = """{"title":"T","sections":[]}""";
        var docFile = Path.Combine(_tempDir, "test.kneeboard");
        await File.WriteAllTextAsync(docFile, json);

        var sut = new DocumentService(new ThrowingRecentDocumentsService());
        var result = await sut.LoadFromPathAsync(docFile);

        Assert.True(result.Success);
        Assert.NotNull(result.Document);
    }

    // ── test doubles ─────────────────────────────────────────────────────────

    private class SpyRecentDocumentsService : IRecentDocumentsService
    {
        public List<RecentDocument> Recorded { get; } = [];

        public Task<IReadOnlyList<RecentDocument>> GetRecentAsync() =>
            Task.FromResult<IReadOnlyList<RecentDocument>>(Recorded);

        public Task RecordOpenedAsync(RecentDocument doc)
        {
            Recorded.Add(doc);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string path) => Task.CompletedTask;
    }

    private class ThrowingRecentDocumentsService : IRecentDocumentsService
    {
        public Task<IReadOnlyList<RecentDocument>> GetRecentAsync() =>
            Task.FromResult<IReadOnlyList<RecentDocument>>([]);

        public Task RecordOpenedAsync(RecentDocument doc) =>
            throw new InvalidOperationException("Simulated recents-store failure.");

        public Task RemoveAsync(string path) => Task.CompletedTask;
    }
}
