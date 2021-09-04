using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class NDSPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("b79501ec-fb56-4a0a-a4ae-018cdf6fecf3");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.nds"};
        public PluginMetadata Metadata { get; }

        public NDSPlugin()
        {
            Metadata = new PluginMetadata("NDS", "onepiecefreak", "NDS Cardridge.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new NDSState();
        }
    }
}
