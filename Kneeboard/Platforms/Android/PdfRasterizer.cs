using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
using Kneeboard.Services;

using JavaFile = Java.IO.File;

namespace Kneeboard.Platforms.Android;

public class PdfRasterizer : IPdfRasterizer
{
    public Task<IReadOnlyList<RenderedPage>> RenderAsync(string pdfPath)
    {
        using var fd = ParcelFileDescriptor.Open(
            new JavaFile(pdfPath),
            ParcelFileMode.ReadOnly);

        using var renderer = new PdfRenderer(fd!);
        var pages = new List<RenderedPage>(renderer.PageCount);

        for (int i = 0; i < renderer.PageCount; i++)
        {
            try
            {
                using var page = renderer.OpenPage(i);
                using var bitmap = Bitmap.CreateBitmap(page.Width, page.Height, Bitmap.Config.Argb8888!)!;
                page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);

                using var ms = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Png!, 100, ms);

                pages.Add(RenderedPage.Ok(ms.ToArray()));
            }
            catch
            {
                // Reported, not dropped — SectionSource substitutes a placeholder so page numbering holds.
                pages.Add(RenderedPage.Failed());
            }
        }

        return Task.FromResult<IReadOnlyList<RenderedPage>>(pages);
    }
}
