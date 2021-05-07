using System;
using System.Linq;
using System.Threading.Tasks;
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

            var sections = ApkSection.ReadAll(fileStream);
            return sections.Count(x => x.Type == ApkSection.PackHeader) == 1;
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new ApkState();
        }
    }
}
