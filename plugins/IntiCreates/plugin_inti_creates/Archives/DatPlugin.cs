using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_inti_creates.Archives
{
    public class DatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("5882b51c-d553-4f8c-9843-6d022f153d99");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.dat"};
        public PluginMetadata Metadata { get; }

        public DatPlugin()
        {
            Metadata=new PluginMetadata("DAT","onepiecefreak","A data resource found in Azure Strikers Gunvokt.");
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new DatState();
        }
    }
}
