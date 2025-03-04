using System.Threading.Tasks;

namespace Herta.Interfaces.IFileService;

public interface IFileService
{
    Task CreateUserFolderAsync(string username);
    Task CleanUpFilesForUserAsync(string username);
    Task SaveFileFromStreamAsync(string username, string fileName, Stream stream);
    Task DeleteFileAsync(string username, string fileName);
    Task<string> GetFilePathAsync(string username, string fileName);
    string GetUserFolderPath(string username);
}