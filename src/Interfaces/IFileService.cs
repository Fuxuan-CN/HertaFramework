using System.Threading.Tasks;
using Herta.Models.DataModels.Users;

namespace Herta.Interfaces.IFileService;

public interface IFileService
{
    Task CreateUserFolderAsync(User user);
    Task CleanUpFilesForUserAsync(User user);
    Task SaveFileFromStreamAsync(User user, string fileName, Stream stream);
    Task<string> GetFilePathAsync(User user, string fileName);
    string GetUserFolderPath(User user);
}