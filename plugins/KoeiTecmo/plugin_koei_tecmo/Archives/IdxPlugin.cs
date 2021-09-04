using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_koei_tecmo.Archives
{
    public class IdxPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("266a0018-e8b7-4921-ab03-e6c639c630ed");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin", "*.idx" };
        public PluginMetadata Metadata { get; }

        public IdxPlugin()
        {
            Metadata = new PluginMetadata("BIN_IDX", "onepiecefreak", "The main resource package in KoeiTecmo games.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new IdxState();
        }
    }
}
