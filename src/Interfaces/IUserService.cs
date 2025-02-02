
using Herta.Models.DataModels.User;

namespace Herta.Interfaces.IUserService
{
    // 用户服务接口
    public interface IUserService
    {
        Task<User> RegisterAsync(string username, string password, string email);
        Task<User> LoginAsync(string username, string password);
        Task ChangePasswordAsync(string username, string oldPassword, string newPassword);
        Task DeleteUserAsync(int userId);
    }
}