
using Herta.Models.DataModels.Users;
using Herta.Models.Forms.DeleteUserForm;

namespace Herta.Interfaces.IUserService
{
    // 用户服务接口
    public interface IUserService
    {
        Task<User> RegisterAsync(string username, string password);
        Task<User> LoginAsync(string username, string password);
        Task ChangePasswordAsync(string username, string oldPassword, string newPassword);
        Task DeleteUserAsync(DeleteUserForm form);
    }
}
