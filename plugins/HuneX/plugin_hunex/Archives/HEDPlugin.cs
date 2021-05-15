using Kontract.Interfaces.Plugins.Identifier;
using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_hunex.Archives
{
    public class HEDPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3102046d-562a-4d81-ae60-828e3ee10e21");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.hed" };
        public PluginMetadata Metadata { get; }

        public HEDPlugin()
        {
            Metadata = new PluginMetadata("HED", "Sn0wCrack; onepiecefreak", "The first main archive for HuneX games.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new HEDState();
        }
    }
}
