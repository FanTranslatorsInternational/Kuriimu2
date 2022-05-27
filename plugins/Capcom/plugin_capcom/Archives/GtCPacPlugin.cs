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
    public class GtCPacPlugin:IFilePlugin
    {
        public Guid PluginId => Guid.Parse("c7e8c80a-53c6-4874-8c45-d74cab6666ac");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public GtCPacPlugin()
        {
            Metadata = new PluginMetadata("GtCPac", "Caleb Mabry", "Handles cpac_2d and cpac_3d files from Ghost Trick  .");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new GtCPacState();
        }
    }
}
