namespace Kneeboard.Services;

public interface IPdfRasterizer
{
    /// <summary>
    /// Rasterizes every page of a PDF to encoded PNG bytes, in page order. A page that fails to
    /// rasterize comes back with <see cref="RenderedPage.Rendered"/> false so the caller can
    /// substitute a placeholder without changing the page count.
    /// </summary>
    Task<IReadOnlyList<RenderedPage>> RenderAsync(string pdfPath);
}
