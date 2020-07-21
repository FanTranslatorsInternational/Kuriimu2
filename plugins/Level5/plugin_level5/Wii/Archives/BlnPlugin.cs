using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_level5.Wii.Archives
{
    public class BlnPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2f02b4dc-6f95-4c6d-b5e8-b70266f8ec2e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bln" };
        public PluginMetadata Metadata { get; }

        public BlnPlugin()
        {
            Metadata = new PluginMetadata("BLN", "onepiecefreak", "Archive in Inazuma Eleven GO Strikers 2013.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new BlnState();
        }
    }
}
