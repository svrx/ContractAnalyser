using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace ContractAnalyser.Utils
{
    public static class ObjectDumpExtensions
    {
        public static string DumpAsYaml(this object o)
        {
            var stringBuilder = new StringBuilder();
            var serializer = new SerializerBuilder()
                .EnsureRoundtrip()
                .Build();
            
            serializer.Serialize(new IndentedTextWriter(new StringWriter(stringBuilder)), o, o.GetType());
            return stringBuilder.ToString();
        }

        public static string DumpAsJson(this object o)
        {
            return JsonConvert.SerializeObject(o);
        }
    }
}
