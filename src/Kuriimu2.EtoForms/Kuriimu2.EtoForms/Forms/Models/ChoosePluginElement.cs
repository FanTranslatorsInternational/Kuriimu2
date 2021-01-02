using System;
using Kontract;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Models;

namespace Kuriimu2.EtoForms.Forms.Models
{
    public class ChoosePluginElement
    {
        public IFilePlugin Plugin { get; }

        public string Name => Plugin.Metadata?.Name ?? "<undefined>";

        public PluginType Type => Plugin.PluginType;

        public Guid PluginId => Plugin.PluginId;

        public ChoosePluginElement(IFilePlugin plugin)
        {
            ContractAssertions.IsNotNull(plugin, nameof(plugin));
            Plugin = plugin;
        }
    }
}
