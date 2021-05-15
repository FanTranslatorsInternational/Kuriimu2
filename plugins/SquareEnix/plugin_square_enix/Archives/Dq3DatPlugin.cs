using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_square_enix.Archives
{
    public class Dq3DatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3015b1c7-ef92-42a3-89ca-e56af26d9d70");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public Dq3DatPlugin()
        {
            Metadata = new PluginMetadata("DQ3 DAT", "onepiecefreak", "The DAT resource found in Dragon Quest 3 on the 3DS.");
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new Dq3DatState();
        }
    }
}
