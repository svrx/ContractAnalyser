using ContractAnalyser.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContractAnalyser.Extractors
{
    public class RoslynMemberNodeExtractor //: IMemberNodeExtractor
    {
        private readonly string assemlbyPath;

        public RoslynMemberNodeExtractor(string assemblyPath)
        {
            this.assemlbyPath = assemblyPath;
        }

        private IAssemblySymbol LoadAssemblySymbol()
        {
            var reference = MetadataReference.CreateFromFile(assemlbyPath);
            var compilation = CSharpCompilation.Create("Mock")
                //.WithOptions(new CSharpCompilationOptions(metadataReferenceResolver: ?) //TODO: Setup metadata reference resolver
                .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
                .AddReferences(reference);

            var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;

            return assembly;
        }

        public Member ExtractNodesData()
        {
            var assembly = LoadAssemblySymbol();
            var visitor = new MemberExtractionSymbolVisitor();
            visitor.Visit(assembly);

            var root = visitor.GetRootMember();

            return root;
        }
    }

    public class MemberExtractionSymbolVisitor : SymbolVisitor
    {
        private INamespaceSymbol currentNamespace = null;
        private Stack<Member> currentMemberStack = new Stack<Member>();
        private Member rootMember = null;

        private Member CurrentMember => currentMemberStack.Count > 0 ? currentMemberStack.Peek() : null;
        public Member GetRootMember() => rootMember;

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            var member = new AssemblyMember()
            {
                Name = symbol.Name,
                Attributes = BuildAttributesLookup(symbol),
                FullName = symbol.ToDisplayString()
            };

            using (AddChildMemberAndEnter(member))
            {
                var globalNamespace = symbol.GlobalNamespace;

                Visit(globalNamespace);

                rootMember = CurrentMember;
            }
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            currentNamespace = symbol;

            //Visit types within namespace
            var types = symbol.GetTypeMembers()
                .Where(p => p.CanBeReferencedByName && p.DeclaredAccessibility >= Accessibility.ProtectedOrInternal);

            Visit(types);

            //Visit child namespaces
            Visit(symbol.GetNamespaceMembers().Where(p=>p.CanBeReferencedByName));
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            var member = new TypeMember()
            {
                Name = symbol.Name,
                Attributes = BuildAttributesLookup(symbol),
                ContainingNamespace = currentNamespace.ToDisplayString(),
                IsPublic = symbol.DeclaredAccessibility == Accessibility.Public,
                IsProtected = symbol.DeclaredAccessibility == Accessibility.Protected || symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal,
                IsStatic = symbol.IsStatic,
                IsSealed = symbol.IsSealed,
                IsVirtual = symbol.IsVirtual,
                IsGeneric = symbol.IsGenericType,
                BaseType = symbol.BaseType?.ToDisplayString(),
                Interfaces = symbol.Interfaces.Select(p=>p.ToDisplayString()).ToList()                
            };            

            using (AddChildMemberAndEnter(member))
            {
                var members = symbol.GetMembers()
                    .Where(p => p.CanBeReferencedByName && p.DeclaredAccessibility >= Accessibility.ProtectedOrInternal);

                Visit(members);

                //Nested Types
                var nestedTypes = symbol.GetTypeMembers()
                    .Where(p => p.CanBeReferencedByName && p.DeclaredAccessibility >= Accessibility.ProtectedOrInternal);

                Visit(nestedTypes);
            }
        }

        public override void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            var member = new TypeParameterMember()
            {
                Name = symbol.ToDisplayString(),
                Attributes = BuildAttributesLookup(symbol),
                //ReturnType
            };

            AddChildMember(member);
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            var member = new MethodMember()
            {
                Name = symbol.Name,
                Attributes = BuildAttributesLookup(symbol),

                ReturnType = symbol.ReturnType?.ToDisplayString(),
                IsPublic = symbol.DeclaredAccessibility == Accessibility.Public,
                IsProtected = symbol.DeclaredAccessibility == Accessibility.Protected || symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal,
                IsStatic = symbol.IsStatic,
                IsVirtual = symbol.IsVirtual,

                IsAsync = symbol.IsAsync,
                IsExtensionMethod = symbol.IsExtensionMethod,
                IsGeneric = symbol.IsGenericMethod
            };            

            using (AddChildMemberAndEnter(member))
            {
                Visit(symbol.TypeParameters); //Used in generics
                Visit(symbol.Parameters);                
            }
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            var member = new PropertyMember()
            {
                Name = symbol.Name,
                Attributes = BuildAttributesLookup(symbol),
                Type = symbol.Type.ToDisplayString(),
                IsPublic = symbol.DeclaredAccessibility == Accessibility.Public,
                IsProtected = symbol.DeclaredAccessibility == Accessibility.Protected || symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal,
                IsStatic = symbol.IsStatic,
                IsVirtual = symbol.IsVirtual,
                IsReadOnly = symbol.IsReadOnly,
                IsWriteOnly = symbol.IsWriteOnly
            };

            AddChildMember(member);
        }

        public override void VisitParameter(IParameterSymbol symbol)
        {
            var member = new ParameterMember()
            {
                Name = symbol.Name,
                Attributes = BuildAttributesLookup(symbol),                
                Type = symbol.Type?.ToDisplayString(),
                HasDefaultValue = symbol.HasExplicitDefaultValue,
                IsOptional = symbol.IsOptional,
                IsParams = symbol.IsParams,
                IsThis = symbol.IsThis,
                IsVirtual = symbol.IsVirtual
            };
            
            AddChildMember(member);
        }

        private Member AddChildMember(Member member)
        {
            if (CurrentMember != null)
                CurrentMember.AddChildren(member);

            return member;
        }

        private DisposableBlock AddChildMemberAndEnter(Member member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            AddChildMember(member);

            var disposable = new DisposableBlock(
                () => { currentMemberStack.Push(member); },
                () => { currentMemberStack.Pop(); }
            );

            return disposable;
        }

        public void Visit(IEnumerable<ISymbol> symbols) => Visit(symbols.ToArray());

        public void Visit(params ISymbol[] symbols)
        {
            foreach (var symbol in symbols)
                symbol.Accept(this);
        }

        public Dictionary<string,IEnumerable<string>> BuildAttributesLookup(ISymbol symbol) => 
            symbol.GetAttributes()
            .ToLookup(
                p => p.AttributeClass.ToDisplayString(),
                p => string.Join(",", 
                    p.ConstructorArguments.Select(a => a.Value)
                    .Concat(
                        p.NamedArguments.Select(a => $"{a.Key}={a.Value.Value}")
                    )
                )
            )
            .Where(p=>!p.Key.StartsWith("System.Runtime.CompilerServices") 
                && !p.Key.StartsWith("System.Diagnostics")
            )
            .ToDictionary(
                p=>p.Key,
                p=>p.ToList().AsEnumerable()
            );

    }
}
