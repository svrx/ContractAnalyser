using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContractAnalyser.Extractors
{
    public class RoslynMemberNodeExtractor : IMemberNodeExtractor
    {
        public RoslynMemberNodeExtractor(string assemlbyPath)
        {
            var reference = MetadataReference.CreateFromFile(assemlbyPath);
            var compilation = CSharpCompilation.Create("TestAssembly").AddReferences(reference);
            
            var symbol = compilation.GetAssemblyOrModuleSymbol(reference);

            var typeSymbol = (symbol as IAssemblySymbol).GlobalNamespace.GetNamespaceMembers().SelectMany(p => p.GetTypeMembers());
            var membersSymbol = (symbol as IAssemblySymbol).GlobalNamespace.GetNamespaceMembers().SelectMany(p => p.GetMembers());

        }

        public MemberNode ExtractNodesData()
        {
            throw new NotImplementedException();
        }
    }
}
