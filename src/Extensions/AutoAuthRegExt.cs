using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Authorize;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Extensions.AutoAuthRegExt
{
    public static class AuthorizationServiceCollectionExtensions
    {
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(AuthorizationServiceCollectionExtensions));

        // 默认扫描当前程序集
        public static IServiceCollection AddAutoAuthorizationPolicies(this IServiceCollection services)
        {
            return AddAutoAuthorizationPolicies(services, Assembly.GetExecutingAssembly());
        }

        // 允许用户指定要扫描的程序集
        public static IServiceCollection AddAutoAuthorizationPolicies(this IServiceCollection services, params Assembly[] assemblies)
        {
            var authReqireTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes<AuthHandlerAttribute>().Any())
                .ToList();

            foreach (var type in authReqireTypes)
            {
                var requirementAttribute = type.GetCustomAttribute<AuthHandlerAttribute>();
                if (requirementAttribute != null)
                {
                    var requirementType = requirementAttribute.RequirementType;

                    var handlerType = type;
                    // 注册 Requirement
                    services.AddScoped(requirementType);

                    // 注册 Handler
                    services.AddScoped(typeof(IAuthorizationHandler), handlerType);

                    // 注册 Policy
                    var policyName = requirementAttribute.PolicyName;
                    _logger.Trace($"register policy {policyName} <=> {handlerType.Name} with requirement {requirementType.Name}");
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy(policyName, policy =>
                        {
                            var inst = Activator.CreateInstance(requirementType) as IAuthorizationRequirement;
                            policy.Requirements.Add(inst!);
                        });
                    });
                }
                else
                {
                    _logger.Trace($"type {type.Name} has no AuthRequirementAttribute");
                }
            }

            return services;
        }
    }
}