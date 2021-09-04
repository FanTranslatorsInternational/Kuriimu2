using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_gust.Archives
{
    public class GzPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("b921e43d-ef03-48ea-bc44-c171ffdda2fb");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.gz" };
        public PluginMetadata Metadata { get; }

        public GzPlugin()
        {
            Metadata = new PluginMetadata("GZ", "onepiecefreak", "An archive resource found in Persona 5 Strikers.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new GzState();
        }
    }
}
