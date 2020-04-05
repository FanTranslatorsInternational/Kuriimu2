using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_level5.Archives
{
    public class PckPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("65178a15-caf5-4f3f-8ece-beb3e4308d0c");
        public string[] FileExtensions => new[] { "*.pck" };
        public PluginMetadata Metadata { get; }

        public PckPlugin()
        {
            Metadata = new PluginMetadata("PCK", "onepiecefreak", "General game archive for 3DS Level-5 games");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new PckState();
        }
    }
}
