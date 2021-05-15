using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_capcom.Archives
{
    public class Gk2Arc1Plugin:IFilePlugin
    {
        public Guid PluginId =>Guid.Parse("fdfbae91-a06d-4443-b1d8-cbb1d84797a1");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public Gk2Arc1Plugin()
        {
            Metadata=new PluginMetadata("GK2_1","onepiecefreak","The main resource archive for Gyakuten Kenji 2.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Gk2Arc1State();
        }
    }
}
