using System.IO;
using System.Reflection;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore
{
    /// <summary>
    /// KoreFileInfo is a simple file state tracking class designed to assist the UI.
    /// </summary>
    public class KoreFileInfo
    {
        public FileInfo FileInfo { get; set; }

        public bool HasChanges { get; set; }

        public ILoadFiles Adapter { get; set; }

        public string DisplayName => FileInfo.Name + (HasChanges ? "*" : string.Empty);

        public string Filter
        {
            get
            {
                var pi = (PluginInfoAttribute)Adapter.GetType().GetCustomAttribute(typeof(PluginInfoAttribute));
                var pei = (PluginExtensionInfoAttribute)Adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute));
                return $"{pi.Name} ({pei.Extension})|{pei.Extension}";
            }
        }
    }
}
