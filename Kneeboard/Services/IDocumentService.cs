using Kneeboard.Models;

namespace Kneeboard.Services;

public interface IDocumentService
{
    Task<DocumentLoadResult> PickAndLoadAsync();
    Task<DocumentLoadResult> LoadFromPathAsync(string filePath);
}

public class DocumentLoadResult
{
    public KneeboardDocument? Document { get; init; }
    public string? Error { get; init; }
    public bool WasCancelled { get; init; }

    public bool Success => Document != null;

    public static DocumentLoadResult Succeeded(KneeboardDocument doc) => new() { Document = doc };
    public static DocumentLoadResult Failed(string error) => new() { Error = error };
    public static DocumentLoadResult Cancelled() => new() { WasCancelled = true };
}
