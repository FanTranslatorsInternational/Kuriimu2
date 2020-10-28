using System;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class MMBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3f6edc1c-215f-4c25-9e06-1bea714e72fe");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public MMBinPlugin()
        {
            Metadata = new PluginMetadata("MMBin", "IcySon55", "2D resource from Mario Maker.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new MMBinState();
        }
    }
}
