using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_mercury_steam.Archives
{
    public class PkgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("63df2b3c-2763-435e-a289-a8444ef1da0d");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pkg" };
        public PluginMetadata Metadata { get; }

        public PkgPlugin()
        {
            Metadata = new PluginMetadata("PKG", "onepiecefreak", "The main archive resource in Metroid: Samus Returns.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new PkgState();
        }
    }
}
