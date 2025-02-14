
namespace Herta.Interfaces.ISecurityPolicy
{
    public interface ISecurityPolicy
    {
        Task<bool> IsRequestAllowed(string ip);
    }
}