using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Herta.Interfaces.ISecurityPolicy;

namespace Herta.Security.MiddlewarePolicy.ExampleSecurityPolicy
{
    public class ExampleSecurityPolicy : ISecurityPolicy
    {

        public ExampleSecurityPolicy()
        {
            // 初始化一些数据结构，比如IP黑名单等
        }
        
        public Task<bool> IsRequestAllowed(string ip)
        {
            // 只是返回true，用于测试
            return Task.FromResult(true);
        }
    }
}