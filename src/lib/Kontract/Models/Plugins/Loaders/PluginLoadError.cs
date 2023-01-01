using System;

namespace Kontract.Models.Plugins.Loaders
{
    public class PluginLoadError
    {
        public string AssemblyPath { get; }

        public Exception Exception { get; }

        public PluginLoadError(string assemblyPath, Exception ex)
        {
            AssemblyPath = assemblyPath;
            Exception = ex;
        }
    }
}
