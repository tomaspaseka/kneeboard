using System.Globalization;
using Kneeboard.Models;

namespace Kneeboard.Converters;

/// <summary>
/// Renders a <see cref="ContentSource"/> as the short label shown under a Global Tab's name in
/// Settings, e.g. "PDF: C:\checklist.pdf" or "Images: C:\charts".
/// </summary>
public class ContentSourceDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            PdfSource pdf => $"PDF: {pdf.Path}",
            ImageFolderSource img => $"Images: {img.Folder}",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Content source descriptions are display-only.");
}
