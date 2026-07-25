namespace Kneeboard.Services;

/// <summary>
/// One page returned by an <see cref="IPdfRasterizer"/>. A page that failed to rasterize is
/// reported explicitly rather than dropped, so page numbering survives a partial failure.
/// </summary>
public readonly record struct RenderedPage(ReadOnlyMemory<byte> Bytes, bool Rendered)
{
    public static RenderedPage Ok(ReadOnlyMemory<byte> bytes) => new(bytes, Rendered: true);

    public static RenderedPage Failed() => new(ReadOnlyMemory<byte>.Empty, Rendered: false);
}
