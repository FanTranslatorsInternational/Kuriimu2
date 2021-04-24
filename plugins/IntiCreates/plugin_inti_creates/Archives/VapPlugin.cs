using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_inti_creates.Archives
{
    public class VapPlugin : IFilePlugin
    {
        public Guid PluginId =>Guid.Parse("e38a0292-5e7d-457f-8795-8e0a1c44900f");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.vap","*.bin" };
        public PluginMetadata Metadata { get; }

        public VapPlugin()
        {
            Metadata = new PluginMetadata("VAP", "onepiecefreak", "An archive for Azure Striker Gunvolt on 3DS.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new VapState();
        }
    }
}
