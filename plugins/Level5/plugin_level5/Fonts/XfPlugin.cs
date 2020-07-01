using System;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;
using plugin_level5.Archives;

namespace plugin_level5.Fonts
{
    public class XfPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a");
        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => new[] { "*.xf" };
        public PluginMetadata Metadata { get; }

        public XfPlugin()
        {
            Metadata = new PluginMetadata("XF", "onepiecefreak", "Font for 3DS Level-5 games");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new XfState(pluginManager);
        }
    }
}
