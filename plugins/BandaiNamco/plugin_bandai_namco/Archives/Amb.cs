using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Archives
{
    class Amb
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(AmbHeader));
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(AmbFileEntry));

        private AmbHeader _header;

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = br.ReadType<AmbHeader>();

            // Read entries
            br.BaseStream.Position = _header.fileEntryStart;
            var entries = br.ReadMultiple<AmbFileEntry>(_header.fileCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < entries.Count; i++)
            {
                var subStream = new SubStream(input, entries[i].offset, entries[i].size);
                var name = $"{i:00000000}{AmbSupport.DetermineExtension(subStream)}";

                result.Add(new AmbArchiveFileInfo(subStream, name, entries[i]));
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Calculate offsets
            var fileEntryOffset = HeaderSize;
            var dataOffset = (fileEntryOffset + files.Count * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            var fileEntries = new List<AmbFileEntry>();

            bw.BaseStream.Position = dataOffset;
            foreach (var file in files.Cast<AmbArchiveFileInfo>())
            {
                var fileOffset = bw.BaseStream.Position;
                var writtenSize = file.SaveFileData(bw.BaseStream);

                fileEntries.Add(new AmbFileEntry
                {
                    offset = (int)fileOffset,
                    size = (int)writtenSize,
                    unk1 = file.Entry.unk1
                });
            }

            bw.WriteAlignment(0x80);

            // Write file entries
            bw.BaseStream.Position = fileEntryOffset;
            foreach (var entry in fileEntries)
                bw.WriteType(entry);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new AmbHeader
            {
                fileEntryStart = fileEntryOffset,
                dataOffset = dataOffset,
                fileCount = files.Count,
                unk1 = _header.unk1
            });
        }
    }
}
