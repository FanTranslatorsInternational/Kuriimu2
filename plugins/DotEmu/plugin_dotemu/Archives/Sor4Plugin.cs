using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_dotemu.Archives
{
    public class Sor4Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("bab218f4-550f-40ee-9219-d83b11265883");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "" };
        public PluginMetadata Metadata { get; }

        public Sor4Plugin()
        {
            Metadata = new PluginMetadata("SOR4", "onepiecefreak", "The main texture resource archive in Streets Of Rage 4.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new Sor4State();
        }
    }
}
