using Kontract;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.WinForms.BatchForms.Model
{
    class PluginElement
    {
        public static PluginElement Empty = new PluginElement(null);

        public bool IsEmpty => Plugin == null;

        public IFilePlugin Plugin { get; }

        public PluginElement(IFilePlugin plugin)
        {
            Plugin = plugin;
        }

        public override string ToString()
        {
            if (Plugin == null)
                return "<No Plugin>";

            return Plugin.Metadata?.Name + " - " + Plugin.PluginId.ToString("D");
        }
    }
}
