using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_primula.Archives
{
    public class Pac2Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("AF5ADDBD-BF3A-4168-A287-BD78C9306DEB");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public Pac2Plugin()
        {
            Metadata = new PluginMetadata("Pac2", "Megaflan", "The main archive resource in Primula games.");
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new Pac2State();
        }
    }
}
