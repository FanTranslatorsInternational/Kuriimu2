using System;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Plugins.Entry;

namespace Kore.Models.UnsupportedPlugin
{
    public class RawPlugin : IFilePlugin
    {
        public static Guid Guid = Guid.Parse("00000001-0000-0000-0000-000000000001");

        public Guid PluginId => Guid;
        public PluginType PluginType => PluginType.Raw;
        public string[] FileExtensions => Array.Empty<string>();
        public PluginMetadata Metadata { get; }

        public RawPlugin()
        {
            Metadata = new PluginMetadata("Default", "onepiecefreak", "No description");
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new RawState();
        }
    }
}
