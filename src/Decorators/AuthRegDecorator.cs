using System;

namespace Herta.Decorators.AuthRegDecorator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AuthHandlerAttribute : Attribute
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