using System.Reflection;
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
