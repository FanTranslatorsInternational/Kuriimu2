using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.Archive;

namespace plugin_test_adapters.Archive
{
    class ArchiveState : IArchiveState, ILoadFiles, ISaveFiles
    {
        private List<ArchiveFileInfo> _files;

        public IReadOnlyList<ArchiveFileInfo> Files => _files.AsReadOnly();

        public bool ContentChanged => Files.Any(afi => afi.ContentChanged);

        public async void Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            _files = new List<ArchiveFileInfo>();

            var inputFile = await fileSystem.OpenFileAsync(filePath);
            using var indexBr = new BinaryReaderX(await fileSystem.OpenFileAsync("index.bin"));
            indexBr.BaseStream.Position = 4;

            var fileCount = indexBr.ReadInt32();

            for (var i = 0; i < fileCount; i++)
            {
                var fileName = i == 0 ? "archive.test" : "index.bin";
                _files.Add(new ArchiveFileInfo(new SubStream(inputFile, indexBr.ReadInt32(), indexBr.ReadInt32()), fileName));
            }
        }

        public async void Save(IFileSystem fileSystem, UPath savePath)
        {
            using var archiveBw = new BinaryWriterX(await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.ReadWrite));
            using var indexBw = new BinaryWriterX(await fileSystem.OpenFileAsync("index.bin", FileMode.Create, FileAccess.ReadWrite));

            archiveBw.WriteString("ARC0", Encoding.UTF8, false, false);
            indexBw.WriteString("IDX0", Encoding.UTF8, false, false);

            indexBw.Write(_files.Count);
            archiveBw.Write(new byte[0xC]);

            foreach (var file in _files)
            {
                indexBw.Write((int)archiveBw.BaseStream.Position);
                indexBw.Write((int)file.FileSize);

                file.SaveFileData(archiveBw.BaseStream, null);
            }
        }
    }
}
