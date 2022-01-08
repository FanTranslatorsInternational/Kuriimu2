using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace Kore.Models.UnsupportedPlugin
{
    public class HexPlugin : IFilePlugin
    {
        public static Guid Guid = Guid.Parse("00000001-0000-0000-0000-000000000001");

        public Guid PluginId => Guid;
        public PluginType PluginType => PluginType.Hex;
        public string[] FileExtensions => Array.Empty<string>();
        public PluginMetadata Metadata { get; }

        public HexPlugin()
        {
            Metadata = new PluginMetadata("Default", "onepiecefreak", "No description");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new HexState();
        }
    }
}
