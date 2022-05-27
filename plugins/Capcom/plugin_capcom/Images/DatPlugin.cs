using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Threading.Tasks;

namespace plugin_capcom.Images
{
    public class DatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("7b8ec4f7-7e9e-4b68-9945-0bcf2299f98a");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public DatPlugin()
        {
            Metadata = new PluginMetadata("DAT", "Caleb Mabry", "Also images for Ghost Trick iOS.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new DatState();
        }
    }
}
