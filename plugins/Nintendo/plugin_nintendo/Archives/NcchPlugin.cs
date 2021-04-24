using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class NcchPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.cxi","*.cfa" };
        public PluginMetadata Metadata { get; }

        public NcchPlugin()
        {
            Metadata = new PluginMetadata("NCCH", "onepiecefreak", "3DS Content Container.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new NcchState();
        }
    }
}
