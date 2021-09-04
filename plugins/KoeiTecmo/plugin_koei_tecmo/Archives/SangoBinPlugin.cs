using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_koei_tecmo.Archives
{
    public class SangoBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("aa6a43ce-ff5f-4bc5-b8b4-fe1b84a5d40e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public SangoBinPlugin()
        {
            Metadata = new PluginMetadata("BIN", "onepiecefreak", "One file archive found in Yo-Kai Watch Sangokushi.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new SangoBinState();
        }
    }
}
