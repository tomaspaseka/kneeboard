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
