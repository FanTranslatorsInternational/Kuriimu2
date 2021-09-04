using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_cattle_call.Archives
{
    public class PackPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("74d25496-ec7b-4a4b-8e68-e2a7dae2b118");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new string[0];
        public PluginMetadata Metadata { get; }

        public PackPlugin()
        {
            Metadata = new PluginMetadata("PACK", "onepiecefreak", "Extensionless pack files in Metal Max 4.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new PackState();
        }
    }
}
