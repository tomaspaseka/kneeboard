namespace Kneeboard.Services;

public interface IPdfService
{
    Task<IReadOnlyList<ImageSource>> RenderAllPagesAsync(string pdfPath);
}
