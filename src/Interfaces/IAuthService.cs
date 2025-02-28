
namespace Herta.Interfaces.IAuthService;

// 授权服务接口
public interface IAuthService
{
    Task<bool> AuthorizeAsync(string token);
    Task<string> GenTokenAsync(Dictionary<string, object> payload);
    Task<bool> ValidateUserAsync(string? userId);
    bool ValidateUser(string? userId);
}
