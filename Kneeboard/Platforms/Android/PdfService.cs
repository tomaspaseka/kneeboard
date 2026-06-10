using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
using Kneeboard.Services;

using JavaFile = Java.IO.File;

namespace Kneeboard.Platforms.Android;

public class PdfService : IPdfService
{
    public Task<IReadOnlyList<ImageSource>> RenderAllPagesAsync(string pdfPath)
    {
        using var fd = ParcelFileDescriptor.Open(
            new JavaFile(pdfPath),
            ParcelFileMode.ReadOnly);

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
