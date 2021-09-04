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

namespace plugin_bandai_namco.Archives
{
    public class ApkPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("082d58ca-f3c6-4bb7-ae9a-b46b97a6bb44");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.apk" };
        public PluginMetadata Metadata { get; }

        public ApkPlugin()
        {
            Metadata = new PluginMetadata("APK", "onepiecefreak", "Main package resource in Gundam 3D Battle.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            fileStream.Position = 0x10;

            var headerCount = 0;
            while (br.ReadString(8) == ApkSection.PackHeader)
            {
                headerCount++;
                fileStream.Position += 0x28;
            }

            return headerCount == 1;
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new ApkState();
        }
    }
}
