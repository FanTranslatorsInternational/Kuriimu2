using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace plugin_capcom.Archives
{
    public class GtPacPlugin:IFilePlugin
    {
        public Guid PluginId => Guid.Parse("c7e8c80a-53c6-4874-8c45-d74cab6819ac");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pac" };
        public PluginMetadata Metadata { get; }

        public GtPacPlugin()
        {
            Metadata = new PluginMetadata("GtPac", "Caleb Mabry", "An unknown resource archive for Ghost Trick motions.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new GtPacState();
        }
    }
}
