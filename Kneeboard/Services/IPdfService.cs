namespace Kneeboard.Services;

public interface IPdfService
{
    /// <summary>
    /// Renders all pages of a PDF to temporary image files and returns the file paths.
    /// </summary>
    Task<IReadOnlyList<string>> RenderAllPagesAsync(string pdfPath);
}
