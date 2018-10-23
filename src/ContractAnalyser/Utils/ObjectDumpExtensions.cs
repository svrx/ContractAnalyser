using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace TypeAnalyser.Utils
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
            //var stringBuilder = new StringBuilder();

            //var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {
            //    DefaultValueHandling = DefaultValueHandling.Ignore,
            //    Formatting = Formatting.Indented
            //});

            //serializer.Serialize(new TextWriter(new StringWriter(stringBuilder)), o);

            return JsonConvert.SerializeObject(o);
        }
    }
}
