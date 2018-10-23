using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TypeAnalyser.Utils;

namespace TypeAnalyser
{
    class Program
    {
        static void Main(string[] args)
        {
            //ThreadPool.SetMinThreads(4, 1);
            var sw = Stopwatch.StartNew();

            var beforeTask = Task.Run(() => AssemblyTypeDataExtractor.ExtractTypesData(@"D:\Workspace\Pleo\Sdk\bin\Pleo.Sdk.dll"));
            var afterTask = Task.Run(() => AssemblyTypeDataExtractor.ExtractTypesData(@"D:\Workspace\Pleo\Sdk\bin\Debug\Pleo.Sdk.dll"));

            Task.WhenAll(beforeTask, afterTask).Wait();

            var beforeRoot = beforeTask.Result;
            var afterRoot = afterTask.Result;

            var diff = GetTreeDiff(beforeRoot, afterRoot);

            var breakingChange = CheckForBreakingChanges(diff);

            if (breakingChange)
            {
                Environment.ExitCode = 2;
                Console.WriteLine("Error! Breaking changes detected.");
            }
            else
                Console.WriteLine("No breaking changes detected. Good Job!");

            sw.Stop();

            Console.WriteLine($"Took {(int)sw.ElapsedMilliseconds}ms.");

            File.WriteAllText(".\\output-before.yaml", beforeRoot.DumpAsYaml());
            File.WriteAllText(".\\output-after.yaml", afterRoot.DumpAsYaml());
            //File.WriteAllText(".\\output-before.json", beforeRoot.DumpAsJson());
            //File.WriteAllText(".\\output-after.json", afterRoot.DumpAsJson());
            File.WriteAllText(".\\diff.yaml", diff.DumpAsYaml());
        }


        private static bool CheckForBreakingChanges(TreeDiff<MemberNode> diff)
        {
            //Run rules to check if delta is a breaking change
            bool breakingChange = false;

            //Additions
            //New parameter added to existing method
            diff.Inserted
                .Where(p => p.Type == MemberTypeNames.Parameter && IsIn(p.Parent.Type, MemberTypeNames.Method, MemberTypeNames.Constructor))
                .ToList()
                .ForEach(p =>
                {
                    breakingChange = true;
                    Console.Error.WriteLine($"ADD: Parameter '{p.Name}' was added/changed to '{p.Parent.Name}'");
                });

            //Removals
            //Removed non-obsolete public type
            diff.Deleted
                .Where(p => !IsObsolete(p)).ToList()
                .ForEach(p =>
                {
                    breakingChange = true;
                    Console.Error.WriteLine($"REM: {p.Type} '{p.Name}' has been removed/changed from '{p.Parent.Name}' and wasn't previously considered obsolete");
                });

            return breakingChange;
        }

        private static bool IsIn<T>(T property, params T[] collection) => collection.Contains(property);

        private static bool IsObsolete(MemberNode data) => data.Attributes.ContainsKey(typeof(ObsoleteAttribute).FullName);


        private static TreeDiff<MemberNode> GetTreeDiff(MemberNode beforeRoot, MemberNode afterRoot)
        {
            var beforeNames = beforeRoot.TraverseNode().ToDictionary(p => p.ToString(), p => p);
            var afterNames = afterRoot.TraverseNode().ToDictionary(p => p.ToString(), p => p);

            var deleted = new HashSet<string>(beforeNames.Keys.Except(afterNames.Keys));
            var inserted = new HashSet<string>(afterNames.Keys.Except(beforeNames.Keys));

            return new TreeDiff<MemberNode>
            {
                Deleted = deleted
                    .Select(p => beforeNames[p])
                    .Where(p => !deleted.Contains(p.Parent.ToString())) //Keep only tree delta roots
                    .ToList(),
                Inserted = inserted.Select(p => afterNames[p])
                    .Where(p => !inserted.Contains(p.Parent.ToString())) //Keep only tree delta roots
                    .ToList()
            };
        }
    }



    public class TreeDiff<T>
        where T:class
    {
        public List<T> Deleted { get; set; }
        public List<T> Inserted { get; set; }
    }

    public static class MemberTypeNames
    {
        public const string Assembly = "Assembly";
        public const string TypeInfo = "TypeInfo";
        public const string Constructor = "Constructor";
        public const string Property = "Property";
        public const string Method = "Method";
        public const string Parameter = "Parameter";
    }
}


