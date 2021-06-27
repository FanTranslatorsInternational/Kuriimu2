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

namespace superflat_games.Images
{
    public class ImgPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("fd64ef73-8c60-43e6-bfef-cf4abc32dd07");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.tex" };
        public PluginMetadata Metadata { get; }

        public ImgPlugin()
        {
            Metadata = new PluginMetadata("TEX", "onepiecefreak", "Main image resource in Lone Survivor.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic1 = br.ReadString(4);
            var size = br.ReadInt32() + 0x2F;

            return magic1 == "IMG0" && fileStream.Length == size;
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new ImgState();
        }
    }
}
