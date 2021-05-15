using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_bandai_namco.Archives
{
    public class _3dsLzPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("863a38e7-69e8-4a53-8045-d864661cb65b");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public _3dsLzPlugin()
        {
            Metadata = new PluginMetadata("3DS-LZ", "onepiecefreak", "The archive used in Dragon Ball Heroes Ultimate Mission");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new _3dsLzState();
        }
    }
}