using System;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;

namespace plugin_test_adapters.Archive
{
    public class ArchivePlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1562cb85-fe0f-4b47-b6e1-017cf8acce68");

        public string[] FileExtensions => new[] { "*.test" };

        public PluginMetadata Metadata { get; }

        public ArchivePlugin()
        {
            Metadata = new PluginMetadata("ArchiveTest", "onepiecefreak3");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new ArchiveState();
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            // Do files exist
            if (!fileSystem.FileExists(filePath) || !fileSystem.FileExists("index.bin"))
                return false;

            // Open files
            var archiveFile = await fileSystem.OpenFileAsync(filePath);
            var indexFile = await fileSystem.OpenFileAsync("index.bin");

            // Identify file
            return CheckTestRequirements(archiveFile, indexFile);
        }

        private bool CheckTestRequirements(Stream archiveFile, Stream indexFile)
        {
            // Test requirements
            // Main file contains "ARC0" magic
            // Index file contains "IDX0" magic
            using var br = new BinaryReaderX(archiveFile);
            using var br2 = new BinaryReaderX(indexFile);

            var magic = br.ReadString(4);
            var magic2 = br2.ReadString(4);

            return magic == "ARC0" && magic2 == "IDX0";
        }
    }
}
