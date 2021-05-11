using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_felistella.Archives
{
    public class PacPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("e7e1f311-fb7e-4be5-bfba-469abe2c927f");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.PAC"};
        public PluginMetadata Metadata { get; }

        public PacPlugin()
        {
            Metadata = new PluginMetadata("PAC", "onepiecefreak", "The package resource in Genkai Tokki Seven Pirates.");
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new PacState();
        }
    }
}
