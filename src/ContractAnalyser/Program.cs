using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using ContractAnalyser.Extractors;
using ContractAnalyser.Utils;

namespace ContractAnalyser
{
    public class Options
    {
        [Value(0, Required = true, HelpText = "Reference base assembly path (older version)")]
        //[Option('b', "base", Required = true, HelpText = "Reference base assembly path (older version)")]
        public string PreviousAssemblyPath { get; set; }

        [Value(1, Required = true, HelpText = "Target assembly path for analysis (newer version)")]
        //[Option('t', "target", Required = true, HelpText = "Target assembly path for analysis (newer version)")]
        public string TargetAssemblyPath { get; set; }

        [Option('r', "ruleset", Required = false, HelpText = "Ruleset name used to check for breaking changes")]
        public string RuleSet { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var parser = Parser.Default;

            parser.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    var extractor = new RoslynMemberNodeExtractor(o.PreviousAssemblyPath);
                    //PerformContactAnalysis(o);
                });            
        }

        private static void PerformContactAnalysis(Options options)
        {
            var measure = new MeasureCommand("Assembly Processing");

            var beforeTask = Task.Run(() => new AssemblyMemberNodeExtractor(options.PreviousAssemblyPath).ExtractNodesData());
            var afterTask = Task.Run(() => new AssemblyMemberNodeExtractor(options.TargetAssemblyPath).ExtractNodesData());

            Task.WhenAll(beforeTask, afterTask).Wait();

            var beforeRoot = beforeTask.Result;
            var afterRoot = afterTask.Result;

            measure.Dispose();

            measure = new MeasureCommand("Detecting Changes");

            var diff = GetTreeDiff(beforeRoot, afterRoot);

            measure.Dispose();

            measure = new MeasureCommand("Evaluating RuleSet");

            bool breakingChange = CheckForBreakingChanges(diff);

            measure.Dispose();

            if (breakingChange)
            {
                Environment.ExitCode = 2;
                Console.WriteLine("Error! Breaking changes detected.");
            }
            else
                Console.WriteLine("No breaking changes detected. Good Job!");


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
                    Console.Error.WriteLine($"ADD: Parameter '{p.UniqueName}' was added/changed to '{p.Parent?.Name}'");
                });

            //Removals
            //Removed non-obsolete public type
            diff.Deleted
                .Where(p => !IsObsolete(p)).ToList()
                .ForEach(p =>
                {
                    breakingChange = true;
                    Console.Error.WriteLine($"REM: {p.Type} '{p.Parent?.Name}.{p.Name}' has been removed/changed and wasn't previously considered obsolete");
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
                    .Where(p => p.Parent == null || !deleted.Contains(p.Parent.ToString())) //Keep only tree delta roots
                    .ToList(),
                Inserted = inserted.Select(p => afterNames[p])
                    .Where(p => p.Parent == null || !inserted.Contains(p.Parent.ToString())) //Keep only tree delta roots
                    .ToList()
            };
        }
    }

    public class MeasureCommand : IDisposable
    {
        private readonly string _actionName;
        private Stopwatch _sw;

        public MeasureCommand(string actionName)
        {
            _sw = Stopwatch.StartNew();            
            _actionName = actionName;
        }

        public void Dispose()
        {
            _sw.Stop();
            if(string.IsNullOrEmpty(_actionName))
                Console.WriteLine($"Took {(int)_sw.ElapsedMilliseconds}ms.");
            else
                Console.WriteLine($"{_actionName} took {(int)_sw.ElapsedMilliseconds}ms.");
        }
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


