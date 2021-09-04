using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_square_enix.Archives
{
    public class Dq3dsDatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3015b1c7-ef92-42a3-89ca-e56af26d9d70");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public Dq3dsDatPlugin()
        {
            Metadata = new PluginMetadata("DQ DAT", "onepiecefreak", "The DAT resource found in Dragon Quest Ports on the 3DS.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new Dq3dsDatState();
        }
    }
}
