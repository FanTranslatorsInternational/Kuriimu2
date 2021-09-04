using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_beeworks.Archives
{
    public class TD3DatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("adc5ff0e-9857-4a3e-8ccb-3b79c4b6f5e8");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public TD3DatPlugin()
        {
            Metadata = new PluginMetadata("Touch Detective 3", "onepiecefreak", "The main resource archive in Touch Detective 3.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new TD3State();
        }
    }
}
