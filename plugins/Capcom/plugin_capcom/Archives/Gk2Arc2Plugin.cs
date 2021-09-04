using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_capcom.Archives
{
    public class Gk2Arc2Plugin:IFilePlugin
    {
        public Guid PluginId =>Guid.Parse("20878c48-697c-46f2-9bbd-5b4b1986dbcc");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public Gk2Arc2Plugin()
        {
            Metadata=new PluginMetadata("GK2_2","onepiecefreak","The sub resource archive for Gyakuten Kenji 2.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new Gk2Arc2State();
        }
    }
}
