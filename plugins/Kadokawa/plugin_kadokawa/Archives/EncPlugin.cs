using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_kadokawa.Archives
{
    public class EncPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("be692456-7296-44ac-91d5-d378dc6c51a3");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.enc" };
        public PluginMetadata Metadata { get; }

        public EncPlugin()
        {
            Metadata = new PluginMetadata("ENC", "onepiecefreak", "An archive for Highschool DxD on 3DS.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new EncState();
        }
    }
}
