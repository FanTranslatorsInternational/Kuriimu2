using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_arc_system_works.Archives
{
    public class FPACPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("101b0e6b-f45f-46e4-9140-98ccad9fa66b");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pac" };
        public PluginMetadata Metadata { get; }

        public FPACPlugin()
        {
            Metadata = new PluginMetadata("FPAC", "onepiecefreak", "The main resource in Arc System Works games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "FPAC";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new FPACState();
        }
    }
}
