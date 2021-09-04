using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Images
{
    public class BimgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("7e60edb3-9a23-4efa-a10b-f113da20d1bc");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bimg" };
        public PluginMetadata Metadata { get; }

        public BimgPlugin()
        {
            Metadata = new PluginMetadata("BIMG", "onepiecefreak", "The thumbnail format for 3DS movies from the eshop.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new BimgState();
        }
    }
}
