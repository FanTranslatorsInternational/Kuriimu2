using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kontract;
using Kontract.Attributes;

namespace Kore.Utilities
{
    public static class Common
    {
        public static string GetAdapterFilter<T>()
        {
            var pi = (PluginInfoAttribute)typeof(T).GetCustomAttribute(typeof(PluginInfoAttribute));
            var pei = (PluginExtensionInfoAttribute)typeof(T).GetCustomAttribute(typeof(PluginExtensionInfoAttribute));
            return pi != null && pei != null ? $"{pi.Name} ({pei.Extension})|{pei.Extension}" : "";
        }

        public static string GetAdapterFilter<T>(T adapter)
        {
            var pi = (PluginInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginInfoAttribute));
            var pei = (PluginExtensionInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute));
            return pi != null && pei != null ? $"{pi.Name} ({pei.Extension})|{pei.Extension}" : "";
        }

        public static string GetAdapterFilters<T>(IEnumerable<T> adapters, string allSupportedFiles = "", bool includeAllFiles = false)
        {
            // Add all of the adapter filters
            var allTypes = adapters.Select(x => new { PluginLoader.Instance.GetMetadata<PluginInfoAttribute>(x).Name, Extension = PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

            // Add the special all supported files filter
            if (allTypes.Count > 0 && !string.IsNullOrEmpty(allSupportedFiles))
                allTypes.Insert(0, new { Name = allSupportedFiles, Extension = string.Join(";", allTypes.Select(x => x.Extension).Distinct()) });

            // Add the special all files filter
            if (includeAllFiles)
                allTypes.Add(new { Name = "All Files", Extension = "*.*" });

            return string.Join("|", allTypes.Select(x => $"{x.Name} ({x.Extension})|{x.Extension}"));
        }

        public static string GetAdapterExtension<T>()
        {
            var pei = (PluginExtensionInfoAttribute)typeof(T).GetCustomAttribute(typeof(PluginExtensionInfoAttribute));
            return pei != null ? $"{pei.Extension.TrimStart('*')}" : "";
        }

        public static string GetAdapterExtension<T>(T adapter)
        {
            var pei = (PluginExtensionInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute));
            return pei != null ? $"{pei.Extension.TrimStart('*')}" : "";
        }
    }
}
