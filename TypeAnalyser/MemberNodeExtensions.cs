using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypeAnalyser
{
    public static class MemberNodeExtensions
    {

        public static Dictionary<string, string> GetAttributesData(this Assembly assembly)
        {
            return assembly.GetCustomAttributesData()
                .Where(p => p.AttributeType != typeof(AsyncStateMachineAttribute))
                .ToDictionary(
                    p => p.AttributeType.FullName,
                    p => string.Join(", ", p.ConstructorArguments.Select(k => k.Value))
                );
        }

        public static Dictionary<string, string> GetAttributesData(this ParameterInfo paramInfo)
        {
            return paramInfo.GetCustomAttributesData()
                .Where(p => p.AttributeType != typeof(AsyncStateMachineAttribute))
                .ToDictionary(
                    p => p.AttributeType.FullName,
                    p => string.Join(", ", p.ConstructorArguments.Select(k => k.Value))
                );
        }
        
        public static MemberNode ToMemberNode(this MemberInfo member)
        {
            return new MemberNode
            {
                Name = member.Name,
                UniqueName = member.ToString(),
                Type = member.MemberType.ToString(),
                Attributes = member.GetCustomAttributesData()
                    .Where(p => p.AttributeType != typeof(AsyncStateMachineAttribute))
                    .ToDictionary(
                        p => p.AttributeType.FullName,
                        p => string.Join(", ", p.ConstructorArguments.Select(k => k.Value))
                    )
            };
        }
    }
}


