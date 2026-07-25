using Kneeboard.Models;

namespace Kneeboard.Services;

public interface ISectionSource
{
    /// <summary>
    /// Reads every page of a section's content source, in order, as encoded image bytes.
    /// Pages are materialized eagerly and held by the caller for as long as the document is open.
    /// </summary>
    /// <exception cref="ArgumentNullException">The section has no content source.</exception>
    /// <exception cref="NotSupportedException">The content source type is not recognised.</exception>
    /// <exception cref="IOException">The underlying file or folder could not be read.</exception>
    Task<IReadOnlyList<ReadOnlyMemory<byte>>> GetPagesAsync(ContentSource? source);
}
