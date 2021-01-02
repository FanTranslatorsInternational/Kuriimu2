using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_ruby_party.Archives
{
    class Cdar
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(CdarHeader));
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(CdarFileEntry));

        private CdarHeader _header;
        private IList<uint> _hashes;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<CdarHeader>();

            // Read hashes
            _hashes = br.ReadMultiple<uint>(_header.entryCount);

            // Read entries
            var entries = br.ReadMultiple<CdarFileEntry>(_header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var name = $"{i:00000000}.bin";

                result.Add(new CdarArchiveFileInfo(subStream, name, Kompression.Implementations.Compressions.ZLib, entry.decompSize));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var hashOffset = HeaderSize;
            var entryOffset = (hashOffset + _hashes.Count * 4 + 0xF) & ~0xF;
            var fileOffset = (entryOffset + files.Count * FileEntrySize + 0xF) & ~0xF;

            // Write files
            output.Position = fileOffset;

            var entries = new List<CdarFileEntry>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                fileOffset = (int)output.Position;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new CdarFileEntry
                {
                    offset = fileOffset,
                    size = (int)writtenSize,
                    decompSize = (int)file.FileSize
                });
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write hashes
            output.Position = hashOffset;
            bw.WriteMultiple(_hashes);

            // Write header
            output.Position = 0;

            _header.entryCount = files.Count;
            bw.WriteType(_header);
        }
    }
}
