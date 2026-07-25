using System.Text;
using Kneeboard.Models;
using Kneeboard.Services;
using Xunit;

namespace Kneeboard.Tests.Services;

public class SectionSourceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public SectionSourceTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // ── image folders ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ImageFolder_ReturnsContentOfEachImage()
    {
        WriteFile("a.png", "first");
        WriteFile("b.png", "second");

        var pages = await Build().GetPagesAsync(Folder());

        Assert.Equal(new[] { "first", "second" }, pages.Select(Decode));
    }

    [Fact]
    public async Task ImageFolder_SortsByFileName()
    {
        WriteFile("c.png", "third");
        WriteFile("a.png", "first");
        WriteFile("b.png", "second");

        var pages = await Build().GetPagesAsync(Folder());

        Assert.Equal(new[] { "first", "second", "third" }, pages.Select(Decode));
    }

    [Theory]
    [InlineData("page.png")]
    [InlineData("page.jpg")]
    [InlineData("page.jpeg")]
    [InlineData("page.bmp")]
    [InlineData("page.gif")]
    [InlineData("page.webp")]
    public async Task ImageFolder_IncludesSupportedExtension(string fileName)
    {
        WriteFile(fileName, "page");

        var pages = await Build().GetPagesAsync(Folder());

        Assert.Equal(new[] { "page" }, pages.Select(Decode));
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("plate.pdf")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    public async Task ImageFolder_ExcludesUnsupportedExtension(string fileName)
    {
        WriteFile("real.png", "page");
        WriteFile(fileName, "ignored");

        var pages = await Build().GetPagesAsync(Folder());

        Assert.Equal(new[] { "page" }, pages.Select(Decode));
    }

    [Fact]
    public async Task ImageFolder_MatchesExtensionCaseInsensitively()
    {
        WriteFile("a.PNG", "upper");
        WriteFile("b.JpEg", "mixed");

        var pages = await Build().GetPagesAsync(Folder());

        Assert.Equal(new[] { "upper", "mixed" }, pages.Select(Decode));
    }

    [Fact]
    public async Task ImageFolder_Empty_ReturnsNoPages()
    {
        var pages = await Build().GetPagesAsync(Folder());

        Assert.Empty(pages);
    }

    [Fact]
    public async Task ImageFolder_Missing_Throws()
    {
        var source = new ImageFolderSource { Folder = Path.Combine(_folder, "gone") };

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => Build().GetPagesAsync(source));
    }

    // ── pdf ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pdf_ReturnsRasterizedPagesInOrder()
    {
        var sectionSource = Build(new FakeRasterizer([Rendered("one"), Rendered("two")]));

        var pages = await sectionSource.GetPagesAsync(new PdfSource { Path = "mission.pdf" });

        Assert.Equal(new[] { "one", "two" }, pages.Select(Decode));
    }

    [Fact]
    public async Task Pdf_FailedPage_SubstitutesPlaceholderAndKeepsPageCount()
    {
        var sectionSource = Build(new FakeRasterizer([Rendered("one"), RenderedPage.Failed(), Rendered("three")]));

        var pages = await sectionSource.GetPagesAsync(new PdfSource { Path = "mission.pdf" });

        Assert.Equal(3, pages.Count);
        Assert.Equal("one", Decode(pages[0]));
        Assert.Equal("three", Decode(pages[2]));
        Assert.True(IsPng(pages[1]), "failed page should be replaced by the placeholder image");
    }

    [Fact]
    public async Task Pdf_EveryPageFails_ReturnsOnePlaceholderPerPage()
    {
        var sectionSource = Build(new FakeRasterizer([RenderedPage.Failed(), RenderedPage.Failed()]));

        var pages = await sectionSource.GetPagesAsync(new PdfSource { Path = "mission.pdf" });

        Assert.Equal(2, pages.Count);
        Assert.All(pages, page => Assert.True(IsPng(page)));
    }

    // ── contract ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NullSource_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => Build().GetPagesAsync(null));

    [Fact]
    public async Task UnknownSourceType_Throws() =>
        await Assert.ThrowsAsync<NotSupportedException>(() => Build().GetPagesAsync(new UnknownSource()));

    // ── helpers ─────────────────────────────────────────────────────────────────

    private void WriteFile(string name, string content) =>
        File.WriteAllText(Path.Combine(_folder, name), content);

    private ImageFolderSource Folder() => new() { Folder = _folder };

    private static SectionSource Build(IPdfRasterizer? rasterizer = null) =>
        new(rasterizer ?? new FakeRasterizer([]));

    private static string Decode(ReadOnlyMemory<byte> page) => Encoding.UTF8.GetString(page.Span);

    private static RenderedPage Rendered(string content) => RenderedPage.Ok(Encoding.UTF8.GetBytes(content));

    private static bool IsPng(ReadOnlyMemory<byte> page) =>
        page.Length > 8 && page.Span[0] == 0x89 && page.Span[1] == (byte)'P'
                        && page.Span[2] == (byte)'N' && page.Span[3] == (byte)'G';

    private sealed class FakeRasterizer(IReadOnlyList<RenderedPage> pages) : IPdfRasterizer
    {
        public Task<IReadOnlyList<RenderedPage>> RenderAsync(string pdfPath) => Task.FromResult(pages);
    }

    private sealed class UnknownSource : ContentSource;
}
