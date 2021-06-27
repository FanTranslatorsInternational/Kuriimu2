using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nippon_ichi.Archives
{
    class Dat
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(DatHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(DatEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<DatHeader>();

            // Read entries
            var entries = br.ReadMultiple<DatEntry>(header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileName = entry.name.Trim('\0');

                result.Add(new DatArchiveFileInfo(fileStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x7FF) & ~0x7FF;

            // Write files
            var entries = new List<DatEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<DatArchiveFileInfo>())
            {
                // Write file data
                output.Position = dataPosition;
                file.SaveFileData(output);

                // Add entry
                entries.Add(new DatEntry
                {
                    offset = dataPosition,
                    size = (int)file.FileSize,
                    name = file.FilePath.GetName(),
                    unk1 = file.Entry.unk1
                });

                dataPosition = (int)((dataPosition + file.FileSize + 0x7FF) & ~0x7FF);
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new DatHeader { fileCount = files.Count });
        }
    }
}
