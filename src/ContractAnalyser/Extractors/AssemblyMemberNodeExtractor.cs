using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ContractAnalyser.Extractors
{
    [Serializable]
    public class AssemblyMemberNodeExtractor : MarshalByRefObject, IMemberNodeExtractor
    {
        private readonly string _assemblyPath;

        public AssemblyMemberNodeExtractor(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        public MemberNode ExtractNodesData()
        {
            return IsolatedAssemblyHandler.ProcessAssembly(_assemblyPath, ExtractAssemblyNode);
        }

        private static MemberNode ExtractAssemblyNode(Assembly assembly)
        {
            var rootNode = new MemberNode()
            {
                Name = assembly.GetName().Name,
                UniqueName = assembly.GetName().Name,
                Type = MemberTypeNames.Assembly,
                Attributes = GetAttributesData(assembly)
            }
            .AddChildren(
                assembly.GetExportedTypes()
                    .OrderBy(p => p.Name)
                    .Select(type => ExtractTypeMemberNode(type))
                    .ToArray()
            );

            return rootNode;
        }

        private static MemberNode ExtractTypeMemberNode(Type type)
        {
            var typeNode = BuildMemberNode(type)
                .AddChildren(
                    type.GetConstructors()
                    .Select(ExtractConstructorMemberNode)
                    .ToArray()
                )
                .AddChildren(
                    type.GetProperties()
                    .Select(BuildMemberNode)
                    .ToArray()
                )
                .AddChildren(
                    type.GetMethods()
                    .Where(p => !p.IsSpecialName && !new[] { typeof(object), typeof(Enum) }.Contains(p.DeclaringType))
                    .Select(ExtractMethodMemberNode)
                    .ToArray()
                );

            return typeNode;
        }

        private static MemberNode ExtractConstructorMemberNode(ConstructorInfo constructor)
        {
            var constructorNode = BuildMemberNode(constructor)
                .AddChildren(
                    constructor.GetParameters()
                    .Select(ExtractParameterMemberNode)
                    .ToArray()
                );

            return constructorNode;
        }

        private static MemberNode ExtractMethodMemberNode(MethodInfo method)
        {
            var methodNode = BuildMemberNode(method)
                .AddChildren(
                    method.GetParameters()
                    .Select(ExtractParameterMemberNode)
                    .ToArray()
                );

            return methodNode;
        }

        private static MemberNode ExtractParameterMemberNode(ParameterInfo param)
        {
            return new MemberNode
            {
                Name = param.Name,
                UniqueName = param.ToString(),
                Type = MemberTypeNames.Parameter,
                Attributes = GetAttributesData(param)
            };
        }

        private static MemberNode BuildMemberNode(MemberInfo member)
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

        private static Dictionary<string, string> GetAttributesData(Assembly assembly)
        {
            return assembly.GetCustomAttributesData()
                .Where(p => p.AttributeType.Namespace != "System.Runtime.CompilerServices")
                .ToDictionary(
                    p => p.AttributeType.FullName,
                    p => string.Join(", ", p.ConstructorArguments.Select(k => k.Value))
                );
        }

        private static Dictionary<string, string> GetAttributesData(ParameterInfo paramInfo)
        {
            return paramInfo.GetCustomAttributesData()
                .Where(p => p.AttributeType.Namespace != "System.Runtime.CompilerServices")
                .ToDictionary(
                    p => p.AttributeType.FullName,
                    p => string.Join(", ", p.ConstructorArguments.Select(k => k.Value))
                );
        }

    }
}


