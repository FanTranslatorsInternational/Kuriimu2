using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace plugin_square_enix.Archives
{
    public class BinState : IArchiveState, ILoadFiles
    {
        // Exposes the loaded files from the archive format to any consuming user interface
        public IList<IArchiveFileInfo> Files { get; private set; }

        // Indicates if the contents of the format were changed
        public bool ContentChanged { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext context)
        {
            var _entries = new List<IArchiveFileInfo>();

            var input = await fileSystem.OpenFileAsync(filePath);
            var reader = new BinaryReaderX(input);

            var header = reader.ReadType<Binheader>();
            var binFiles = reader.ReadMultiple<BinTableEntry>(header.fileCount);
            for (int i = 0; i < binFiles.Count; i++)
            {
                using (var sub = new SubStream(input, binFiles[i].offset, binFiles[i].fileSize))
                _entries.Add(new ArchiveFileInfo(sub, $"file{i}"));
            }
            Files = _entries;
        }

    }

}
