using Kneeboard.Models;

namespace Kneeboard.Services;

public class SectionSource : ISectionSource
{
    /// <summary>Shown in place of a PDF page that failed to rasterize. Embedded so it resolves
    /// identically inside the app package and in tests, which run outside MSIX.</summary>
    private const string PlaceholderResourceName = "Kneeboard.Assets.page-render-failed.png";

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"];

    private static readonly Lazy<ReadOnlyMemory<byte>> Placeholder = new(LoadPlaceholder);

    private readonly IPdfRasterizer _rasterizer;

    public SectionSource(IPdfRasterizer rasterizer) => _rasterizer = rasterizer;

    public async Task<IReadOnlyList<ReadOnlyMemory<byte>>> GetPagesAsync(ContentSource? source) =>
        source switch
        {
            PdfSource pdf => await RenderPdfAsync(pdf.Path),
            ImageFolderSource images => await ReadImageFolderAsync(images.Folder),
            null => throw new ArgumentNullException(nameof(source), "Section has no content source."),
            _ => throw new NotSupportedException($"Unsupported content source: {source.GetType().Name}.")
        };

    private async Task<IReadOnlyList<ReadOnlyMemory<byte>>> RenderPdfAsync(string pdfPath)
    {
        var rendered = await _rasterizer.RenderAsync(pdfPath);
        return [.. rendered.Select(page => page.Rendered ? page.Bytes : Placeholder.Value)];
    }

    private static async Task<IReadOnlyList<ReadOnlyMemory<byte>>> ReadImageFolderAsync(string folder)
    {
        var files = Directory.GetFiles(folder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Order()
            .ToList();

        var pages = new List<ReadOnlyMemory<byte>>(files.Count);
        foreach (var file in files)
            pages.Add(await File.ReadAllBytesAsync(file));

        return pages;
    }

    private static ReadOnlyMemory<byte> LoadPlaceholder()
    {
        using var stream = typeof(SectionSource).Assembly.GetManifestResourceStream(PlaceholderResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{PlaceholderResourceName}' not found.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
