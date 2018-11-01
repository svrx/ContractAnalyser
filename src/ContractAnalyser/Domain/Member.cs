using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ContractAnalyser.Domain
{

    [Serializable]
    public class Member
    {
        public string Name { get; set; }
        public MemberKind Kind { get; set; }
        public IDictionary<string, IEnumerable<string>> Attributes { get; set; }        

        [JsonIgnore, YamlIgnore]
        public Member Parent { get; set; }
        public List<Member> Children { get; set; } = new List<Member>(6);

        public Member() { }

        protected Member(MemberKind kind)
        {
            Kind = kind;
        }

        public Member AddChildren(params Member[] childNodes)
        {
            foreach (var childNode in childNodes)
                childNode.Parent = this;

            Children.AddRange(childNodes);

            return this;
        }

        //public override string ToString()
        //{
        //    return $"{{{(Parent != null ? $"{Parent}->" : "")}}}->{Name}:{Kind}";
        //}
    }

    [Serializable]
    public class AssemblyMember : Member
    {
        public string FullName { get; set; }

        //Computed
        public string Version => 
            Attributes.ContainsKey(typeof(System.Reflection.AssemblyFileVersionAttribute).FullName) 
            ? Attributes[typeof(System.Reflection.AssemblyFileVersionAttribute).FullName].FirstOrDefault() 
            : null;
        public string TargetFramework =>
            Attributes.ContainsKey(typeof(System.Runtime.Versioning.TargetFrameworkAttribute).FullName) 
            ? Attributes[typeof(System.Runtime.Versioning.TargetFrameworkAttribute).FullName].FirstOrDefault()
            : null;

        public AssemblyMember() : base(MemberKind.Assembly) { }

        public override string ToString() => FullName;
    }

    [Serializable]
    public class TypeMember : Member
    {
        public string BaseType { get; set; }            //TODO: Consider to full member as type
        public List<string> Interfaces { get; set; }    //TODO: Consider to full member as type
        public string ContainingNamespace { get; set; }

        public bool IsPublic { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsSealed { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsGeneric { get; set; }

        public TypeMember() : base(MemberKind.NamedType) { }

        private string GetTypeParametersString() =>
            IsGeneric
            ? "<" + string.Join(", ", Children.OfType<TypeParameterMember>().Select(p => p.ToString())) + ">"
            : string.Empty;

        public override string ToString() => $"{ContainingNamespace}.{Name}{GetTypeParametersString()}";
    }

    [Serializable]
    public class PropertyMember : Member
    {
        public string Type { get; set; }

        public bool IsPublic { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }

        public bool IsReadOnly { get; set; }
        public bool IsWriteOnly { get; set; }

        //Computed
        [JsonIgnore, YamlIgnore]
        public TypeMember ContainingType => Parent as TypeMember;

        public PropertyMember() : base(MemberKind.Property) { }

        public override string ToString() => $"{Type} {Name}";
    }

    [Serializable]
    public class MethodMember : Member
    {
        public string ReturnType { get; set; }

        public bool IsPublic { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }

        public bool IsAsync { get; set; }
        public bool IsExtensionMethod { get; set; }
        public bool IsGeneric { get; set; }


        //Computed
        public bool ReturnsVoid => ReturnType == "void";

        [JsonIgnore, YamlIgnore]
        public TypeMember ContainingType => Parent as TypeMember;

        public MethodMember() : base(MemberKind.Method) { }

        private string GetTypeParametersString() =>
            IsGeneric
            ? "<" + string.Join(", ", Children.OfType<TypeParameterMember>().Select(p => p.ToString())) + ">"
            : string.Empty;

        private string GetParametersString() =>
            string.Join(", ", Children.OfType<ParameterMember>().Select(p => p.ToString()));

        public override string ToString() => $"{ReturnType} {Name}{GetTypeParametersString()}({GetParametersString()})";
    }

    [Serializable]
    public class TypeParameterMember : Member
    {
        [JsonIgnore, YamlIgnore]
        public TypeMember DeclaringType => Parent as TypeMember;

        [JsonIgnore, YamlIgnore]
        public MethodMember DeclaringMethod => Parent as MethodMember;

        public TypeParameterMember() : base(MemberKind.TypeParameter) { }

        public override string ToString() => $"{Name}";
    }

    [Serializable]
    public class ParameterMember : Member
    {
        public string Type { get; set; }
        public bool HasDefaultValue { get; set; }

        public bool IsOptional { get; set; }
        public bool IsParams { get; set; }
        public bool IsThis { get; set; }
        public bool IsVirtual { get; set; }

        [JsonIgnore, YamlIgnore]
        public TypeMember ContainingType => Parent as TypeMember;

        [JsonIgnore, YamlIgnore]
        public MethodMember ContainingMethod => Parent as MethodMember;

        public ParameterMember() : base(MemberKind.Parameter) { }

        private string GetParameterModifierString() =>
            IsParams
            ? "params "
            : (IsThis
                ? "this "
                : string.Empty
            );

        public override string ToString() => $"{GetParameterModifierString()}{Type} {Name}";
    }
}
