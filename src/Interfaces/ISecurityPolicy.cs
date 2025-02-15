using Microsoft.AspNetCore.Http;

namespace Herta.Interfaces.ISecurityPolicy
{
    public interface ISecurityPolicy
    {
        Task<bool> IsRequestAllowed(HttpContext context);
    }
}