using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Kneeboard.Services;

namespace Kneeboard.Platforms.Windows;

public class PdfRasterizer : IPdfRasterizer
{
    public async Task<IReadOnlyList<RenderedPage>> RenderAsync(string pdfPath)
    {
        var file = await StorageFile.GetFileFromPathAsync(pdfPath);
        var pdfDocument = await PdfDocument.LoadFromFileAsync(file);
        var pages = new List<RenderedPage>((int)pdfDocument.PageCount);

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

                pages.Add(RenderedPage.Ok(bytes));
            }
            catch
            {
                // Reported, not dropped — SectionSource substitutes a placeholder so page numbering holds.
                pages.Add(RenderedPage.Failed());
            }
        }

        return pages;
    }
}
