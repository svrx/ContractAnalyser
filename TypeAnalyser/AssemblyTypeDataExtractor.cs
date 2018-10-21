using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TypeAnalyser.Utils;

namespace TypeAnalyser
{
    [Serializable]
    public class AssemblyTypeDataExtractor : MarshalByRefObject
    {
        public static MemberNode ExtractTypesData(string assemblyPath)
        {
            using (var isolated = new Isolated<AssemblyTypeDataExtractor>())
            {
                return isolated.Value.ExtractTypesDataInternal(assemblyPath);
            }
        }

        private MemberNode ExtractTypesDataInternal(string assemblyPath)
        {
            SetupReflectionOnlyResolverHandler();
            var beforeAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            var root = BuildTypesDataTree(beforeAssembly);

            return root;
        }

        private static MemberNode BuildTypesDataTree(Assembly assembly)
        {
            var rootNode = new MemberNode()
            {
                Name = assembly.GetName().Name,
                UniqueName = assembly.ToString(),
                Type = MemberTypeNames.Assembly,
                Attributes = assembly.GetAttributesData()
            };
                       
            rootNode.AddChildren(
                assembly.GetExportedTypes()
                    .OrderBy(p => p.Name)
                    .Select(type => BuildTypeMemberNode(type))
                    .ToArray()
            );

            return rootNode;
        }

        private static MemberNode BuildTypeMemberNode(Type type)
        {
            var typeNode = type.ToMemberNode()
                .AddChildren(
                    type.GetConstructors()
                    .Select(BuildConstructorMemberNode)
                    .ToArray()
                )
                .AddChildren(
                    type.GetProperties()
                    .Select(MemberNodeExtensions.ToMemberNode)
                    .ToArray()
                )
                .AddChildren(
                    type.GetMethods()
                    .Where(p => !p.IsSpecialName && !new[] { typeof(object), typeof(Enum) }.Contains(p.DeclaringType))
                    .Select(BuildMethodMemberNode)
                    .ToArray()
                );

            return typeNode;
        }

        private static MemberNode BuildConstructorMemberNode(ConstructorInfo constructor)
        {
            var constructorNode = constructor.ToMemberNode()
                .AddChildren(
                    constructor.GetParameters()
                    .Select(BuildParameterMemberNode)
                    .ToArray()
                );

            return constructorNode;
        }

        private static MemberNode BuildMethodMemberNode(MethodInfo method)
        {
            var methodNode = method.ToMemberNode()
                .AddChildren(
                    method.GetParameters()
                    .Select(BuildParameterMemberNode)
                    .ToArray()
                );

            return methodNode;
        }

        private static MemberNode BuildParameterMemberNode(ParameterInfo param)
        {
            return new MemberNode
            {
                Name = param.Name,
                UniqueName = param.ToString(),
                Type = MemberTypeNames.Parameter,
                Attributes = param.GetAttributesData()
            };
        }

        private static void SetupReflectionOnlyResolverHandler()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (object sender, ResolveEventArgs e) =>
            {
                var dir = Path.GetDirectoryName(e.RequestingAssembly.Location);
                var path = $"{dir}\\{e.Name.Split(',')[0]}.dll";

                if (File.Exists(path))
                    return Assembly.ReflectionOnlyLoadFrom(path);

                return Assembly.ReflectionOnlyLoad(e.Name);
            };
        }
    }
}


