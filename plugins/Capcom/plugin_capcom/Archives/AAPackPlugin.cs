using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_capcom.Archives
{
    public class AAPackPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("8a86b7a5-7a3b-4d7f-95e6-31c417b0f4a8");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.inc", "*.dat" };
        public PluginMetadata Metadata { get; }

        public AAPackPlugin()
        {
            Metadata = new PluginMetadata("AAPack", "onepiecefreak", "The main archive of Ace Attorney Trilogy and Apollo Justice 3DS.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new AAPackState();
        }
    }
}
