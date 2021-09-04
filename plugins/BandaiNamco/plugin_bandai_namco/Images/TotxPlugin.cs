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

namespace plugin_bandai_namco.Images
{
    public class TotxPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId { get; }
        public PluginType PluginType { get; }
        public string[] FileExtensions { get; }
        public PluginMetadata Metadata { get; }

        public TotxPlugin()
        {
            Metadata=new PluginMetadata("TOTX","onepiecefreak","Image resource found in FileArc.bin's of Dragon Ball Heroes games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "TOTX";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new TotxState(fileManager);
        }
    }
}
