using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_felistella.Images
{
    public class TexPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("4a29ddcc-bf9b-4fba-a5cd-6291fed13f23");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.tex_vita"};
        public PluginMetadata Metadata { get; }

        public TexPlugin()
        {
            Metadata=new PluginMetadata("TEX_VITA","onepiecefreak","The main image resource in Genka Tikko Seven Pirates.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new TexState();
        }
    }
}
