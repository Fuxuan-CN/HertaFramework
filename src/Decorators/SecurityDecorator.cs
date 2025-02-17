using System;
using Herta.Security.MiddlewarePolicy.ExampleSecurityPolicy;
using Herta.Interfaces.ISecurityPolicy;

namespace Herta.Decorators.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SecurityProtectAttribute : Attribute
    {
        public bool EnableSecurity { get; set; } = true; // 是否启用安全策略
        public Type PolicyType { get; set; } = typeof(ExampleSecurityPolicy); // 自定义策略类型（可选）

        public SecurityProtectAttribute(bool enableSecurity, Type policyType)
        {
            EnableSecurity = enableSecurity;
            PolicyType = policyType;

            if (PolicyType != null && !typeof(ISecurityPolicy).IsAssignableFrom(PolicyType))
            {
                throw new ArgumentException("PolicyType must implement ISecurityPolicy interface.");
            }
        }
    }
}