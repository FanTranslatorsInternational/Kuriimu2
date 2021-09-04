using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_atlus.Archives
{
    public class DsPspBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("97C4C1A0-F375-49CD-AA3E-2621A6827D0B");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public DsPspBinPlugin()
        {
            Metadata = new PluginMetadata("DsPspBin", "Megaflan", "The Bin resource container seen in Shin Megami Tensei: Devil Summoner (PSP).");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new DsPspBinState();
        }
    }
}
