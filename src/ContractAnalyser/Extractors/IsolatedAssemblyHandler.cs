using System;
using System.IO;
using System.Reflection;
using ContractAnalyser.Utils;

namespace ContractAnalyser.Extractors
{
    public class IsolatedAssemblyHandler: MarshalByRefObject
    {
        /// <summary>
        /// Loads specified assembly in separate AppDomain as reflection only and executes specified function
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="assemblyPath"></param>
        /// <param name="processorFunc"></param>
        /// <returns></returns>
        public static TReturn ProcessAssembly<TReturn>(string assemblyPath, Func<Assembly,TReturn> processorFunc)
        {
            using (var isolated = new Isolated<IsolatedAssemblyHandler>())
            {
                return isolated.Value.ProcessAssemblyInternal(assemblyPath, processorFunc);
            }            
        }
        
        private TReturn ProcessAssemblyInternal<TReturn>(string assemblyPath, Func<Assembly, TReturn> processorFunc) {
            EnsureAssemblyResolveHandlerSetup();
            
            var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);

            return processorFunc(assembly);
        }

        bool _resolverHandlerSetup;
        private void EnsureAssemblyResolveHandlerSetup()
        {
            if (!_resolverHandlerSetup)
            {
                SetupReflectionOnlyResolverHandler();
                _resolverHandlerSetup = true;
            }
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


