using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_inti_creates.Archives
{
    public class FntPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("6f8375b9-9ee4-4168-87eb-9203da6c4000");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.fnt" };
        public PluginMetadata Metadata { get; }

        public FntPlugin()
        {
            Metadata = new PluginMetadata("FNT", "onepiecefreak", "An archive for Azure Striker Gunvolt on 3DS.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new FntState();
        }
    }
}
