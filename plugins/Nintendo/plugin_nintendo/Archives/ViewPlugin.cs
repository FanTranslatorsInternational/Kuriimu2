using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class ViewPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("a32c6750-7907-4abc-b009-47a5b6fd1251");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.viw" };
        public PluginMetadata Metadata { get; }

        public ViewPlugin()
        {
            Metadata = new PluginMetadata("VIW", "onepiecefreak", "The lib resource from Tingle Baloon Trip.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new ViwState();
        }
    }
}
