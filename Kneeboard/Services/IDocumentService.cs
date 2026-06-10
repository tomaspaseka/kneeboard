using Kneeboard.Models;

namespace Kneeboard.Services;

public interface IDocumentService
{
    Task<DocumentLoadResult> PickAndLoadAsync();
    Task<DocumentLoadResult> LoadFromPathAsync(string filePath);
}
