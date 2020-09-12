using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_inti_creates.Archives
{
    class Vap
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(VapHeader));
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(VapFileEntry));

        private VapHeader _header;

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<VapHeader>();

            // Read entries
            var entries = br.ReadMultiple<VapFileEntry>(_header.fileCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var name = $"{i:00000000}{VapSupport.DetermineExtension(subStream)}";

                result.Add(new VapArchiveFileInfo(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = (HeaderSize + files.Count * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            bw.BaseStream.Position = fileOffset;

            var entries = new List<VapFileEntry>();
            foreach (var file in files.Cast<VapArchiveFileInfo>())
            {
                fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.SaveFileData(output);

                if (file != files.Last())
                    bw.WriteAlignment(0x80);

                entries.Add(new VapFileEntry
                {
                    offset = fileOffset,
                    size = (int)writtenSize,

                    unk1 = file.Entry.unk1,
                    unk2 = file.Entry.unk2
                });
            }

            // Write entries
            bw.BaseStream.Position = HeaderSize;
            bw.WriteMultiple(entries);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new VapHeader
            {
                fileCount = files.Count,
                unk1 = _header.unk1
            });
        }
    }
}
