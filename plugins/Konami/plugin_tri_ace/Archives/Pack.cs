using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_tri_ace.Archives
{
    // Game: Beyond The Labyrinth
    class Pack
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PackHeader));
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(PackFileEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<PackHeader>();

            // Read entries
            var entries = br.ReadMultiple<PackFileEntry>(header.fileCount + 1);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entries[i + 1].offset - entry.offset);
                var name = $"{i:00000000}{PackSupport.DetermineExtension(entry.fileType)}";

                result.Add(new PackArchiveFileInfo(subStream, name, entry)
                {
                    PluginIds = PackSupport.RetrievePluginMapping(entries[i].fileType)
                });
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = (entryOffset + (files.Count + 1) * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            output.Position = fileOffset;

            var entries = new List<PackFileEntry>();
            foreach (var file in files.Cast<PackArchiveFileInfo>())
            {
                fileOffset = (int)output.Position;
                file.SaveFileData(output);

                bw.WriteAlignment();

                entries.Add(new PackFileEntry
                {
                    offset = fileOffset,
                    fileType = file.Entry.fileType,
                    unk0 = file.Entry.unk0
                });
            }

            // Write end file/blob
            entries.Add(new PackFileEntry
            {
                offset = (int)output.Position
            });
            bw.WritePadding(0x80);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new PackHeader
            {
                fileCount = (short)files.Count
            });
        }
    }
}
