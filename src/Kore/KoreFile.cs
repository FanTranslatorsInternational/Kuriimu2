using System.IO;
using System.Reflection;
using Kontract.Attribute;
using Kontract.Interface;

namespace Kore
{
    /// <summary>
    /// KoreFile is a simple file state tracking class designed to assist the UI.
    /// </summary>
    public class KoreFile
    {
        public FileInfo FileInfo { get; set; }

        public bool HasChanges { get; set; }

        public ILoadFiles Adapter { get; set; }

        public string Filter
        {
            get
            {
                var pi = (PluginInfo)Adapter.GetType().GetCustomAttribute(typeof(PluginInfo));
                var pei = (PluginExtensionInfo)Adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfo));
                return $"{pi.Name} ({pei.Extension})|{pei.Extension}";
            }
        }
    }
}
