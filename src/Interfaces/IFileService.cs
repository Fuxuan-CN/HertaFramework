using System.Threading.Tasks;

namespace Herta.Interfaces.IFileService;

public interface IFileService
{
    Task CreateUserFolderAsync(int userId);
    Task CleanUpFilesForUserAsync(int userId);
    Task SaveFileFromStreamAsync(int userId, string fileName, Stream stream);
    Task DeleteFileAsync(int userId, string fileName);
    Task<string> GetFilePathAsync(int userId, string fileName);
    string GetUserFolderPath(int userId);
}