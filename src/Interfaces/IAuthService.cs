
namespace Herta.Interfaces.IAuthService;

// 授权服务接口
public interface IAuthService
{
    Task<bool> AuthorizeAsync(string token);
    Task<string> GenTokenAsync(Dictionary<string, object> payload);
    Task<bool> ValidateUserAsync(int userId);
    bool ValidateUser(int userId);
}
