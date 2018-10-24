using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace ContractAnalyser
{
    [Serializable]
    public class MemberNode
    {
        public string UniqueName { get; set; }
        /// <summary>
        /// Local name
        /// </summary>
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        
        [JsonIgnore, YamlIgnore]
        public MemberNode Parent { get; set; }
        public List<MemberNode> Children { get; set; } = new List<MemberNode>(6);

        public MemberNode AddChildren(params MemberNode[] childNodes)
        {
            foreach (var childNode in childNodes)
                childNode.Parent = this;

            Children.AddRange(childNodes);

            return this;
        }

        public override string ToString()
        {
            var parentName = Parent != null ? Parent.ToString() : "Root";
            return $"{parentName}->{UniqueName}:{Type}";
        }


        public IEnumerable<MemberNode> TraverseNode()
        {
            return Traverse(this, p => p.Children);
        }
            

        private static IEnumerable<T> Traverse<T>(T item, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>();
            stack.Push(item);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }   
    }
}


