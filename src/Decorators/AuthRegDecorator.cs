using System;

namespace Herta.Decorators.AuthorizeRegister
{
    // 注册一个类为认证处理器，指定名字和需求类型，并在启动的时候自动注册到容器中
    // 然后就可以用 [AuthHandler(typeof(CustomRequirement), "CustomPolicy")] 这样的方式来进行认证了
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AuthHandlerAttribute : Attribute
    {
        public Type RequirementType { get; }
        public string PolicyName { get; set; }

        public AuthHandlerAttribute(Type requirementType, string policyName = "CustomPolicy")
        {
            RequirementType = requirementType;
            PolicyName = policyName;
        }
    }
}
