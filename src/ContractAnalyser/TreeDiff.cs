using System.Collections.Generic;

namespace ContractAnalyser
{
    public class TreeDiff<T>
        where T:class
    {
        public List<T> Deleted { get; set; }
        public List<T> Inserted { get; set; }
    }
}


