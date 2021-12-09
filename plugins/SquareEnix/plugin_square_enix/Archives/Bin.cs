using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace plugin_square_enix.Archives
{
    class Bin
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            var _entries = new List<IArchiveFileInfo>();
            var reader = new BinaryReaderX(input);

            var header = reader.ReadType<Binheader>();
            var binFiles = reader.ReadMultiple<BinTableEntry>(header.fileCount);
            for (int i = 0; i < binFiles.Count; i++)
            {
                using (var sub = new SubStream(input, binFiles[i].offset, binFiles[i].fileSize))
                    _entries.Add(new ArchiveFileInfo(sub, $"file{i}"));
            }
            return _entries;
        }
        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {

        }
    }
}
