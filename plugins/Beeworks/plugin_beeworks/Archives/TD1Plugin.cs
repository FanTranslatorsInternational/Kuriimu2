using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_beeworks.Archives
{
    public class TD1Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("e8b9f059-7321-4aff-bbb1-a55e06d0bd9f");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public TD1Plugin()
        {
            Metadata=new PluginMetadata("Touch Detective 1","onepiecefreak","The main archive for Touch Detective 1.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new TD1State();
        }
    }
}
