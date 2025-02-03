using System;

namespace Herta.Decorators.AuthRegDecorator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AuthRequirementAttribute : Attribute
    {
        public Type RequirementType { get; }
        public string PolicyName { get; set; }

        public AuthRequirementAttribute(Type requirementType, string policyName = "CustomPolicy")
        {
            RequirementType = requirementType;
            PolicyName = policyName;
        }
    }
}